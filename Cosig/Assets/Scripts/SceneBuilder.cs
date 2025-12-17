using Models;
using Services;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(GPUPrimaryRays))]
public class SceneBuilder : MonoBehaviour
{
    [SerializeField] private Material baseMaterial;

    private readonly SceneService sceneService = new();

    private List<ObjectData> sceneObjects = new();
    private List<LightData> lights = new();
    private List<Transformation> transformations = new();
    private List<MaterialProperties> materials = new();

    [SerializeField] private RawImage outputImage;
    
    private ImageSettings imgSettings;

    private BVHBuilder bvhBuilder;

    private GPUPrimaryRays gpuRayTracer;
    private PrimaryRays cpuRayTracer;

    public event System.Action OnSceneLoaded;

    private int rec = 2;
    private CameraData cameraData;
    private Camera cameraGO;

    private readonly List<GameObject> instantiatedObjects = new();

    private float elapsedTimeMS = 0f;

    private bool hasAmbient = true, hasDiffuse = true, hasSpecular = true, hasRefraction = true;

    void Start()
    {
        gpuRayTracer = GetComponent<GPUPrimaryRays>();
    }

    public void BuildScene()
    {
        foreach (var objData in sceneObjects)
        {
            GameObject obj = null;
            if (objData is SphereData) obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            else if (objData is BoxData) obj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            else if (objData is TrianglePrimitive triData)
            {
                obj = new GameObject("Triangle");
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();

                Mesh mesh = new();
                mf.mesh = mesh;

                Vector3[] vertices = new Vector3[]
                {
                    triData.v1,
                    triData.v2,
                    triData.v3
                };

                Vector2[] uvs = new Vector2[]
                {
                        new(0, 0),
                        new(0, 1),
                        new(1, 1)
                };


                int[] triangles = new int[] { 0, 1, 2 };
                Vector3 normal = Vector3.Cross(triData.v2 - triData.v1, triData.v3 - triData.v1).normalized;
                Vector3[] normals = new Vector3[] { normal, normal, normal };
                mesh.Clear();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                mesh.normals = normals;

                mesh.RecalculateBounds();
            }

            else if (objData is CameraData camData)
            {
                var camObj = new GameObject("Camera");
                cameraGO = camObj.AddComponent<Camera>();
                cameraGO.fieldOfView = camData.fov;

                cameraData = camData;

                // aplicar transformação base
                var t = transformations[camData.transformationIndex];
                ApplyTransformation(camObj, t);

                // aplicar distance como offset LOCAL
                camObj.transform.Translate(Vector3.back * camData.distance, Space.Self);

                obj = camObj;
            }


            else if (objData is ImageSettings imgData)
                imgSettings = imgData;

            int tIndex = objData switch
            {
                SphereData s => s.transformationIndex,
                BoxData b => b.transformationIndex,
                TrianglePrimitive t => t.transformationIndex,
                CameraData c => c.transformationIndex,
                _ => -1
            };

            if (tIndex >= 0 && tIndex < transformations.Count)
            {
                ApplyTransformation(obj, transformations[tIndex]);
            }

            // Identifica e aplica material
            int mIndex = objData switch
            {
                SphereData s => s.materialIndex,
                BoxData b => b.materialIndex,
                TrianglePrimitive t => t.materialIndex,
                _ => -1
            };
            if (mIndex >= 0 && mIndex < materials.Count)
            {
                ApplyMaterial(obj, materials[mIndex]);
            }
            instantiatedObjects.Add(obj);
        }
        foreach(var lightData in lights)
        {
            var lightObj = new GameObject("Light");
            var light = lightObj.AddComponent<Light>();
            light.color = lightData.color;
            ApplyTransformation(lightObj, transformations[lightData.transformationIndex]);
            instantiatedObjects.Add(lightObj);
        }
    }

    public void ApplyTransformation(GameObject obj, Transformation transformation)
    {
        if (transformation == null) return;
        obj.transform.Translate(transformation.translation, Space.World);
        obj.transform.Rotate(transformation.rotation);
        obj.transform.localScale = transformation.scale;
    }

    void ApplyMaterial(GameObject obj, MaterialProperties properties)
    {
        Material newMaterial = new(baseMaterial)
        {
            color = properties.color
        };

        if (obj.TryGetComponent<Renderer>(out var renderer)) renderer.material = newMaterial;
    }

    private GPUObject[] ConvertObjectsToGPU()
    {
        GPUObject[] gpuObjects = new GPUObject[sceneObjects.Count];

        for (int i = 0; i < sceneObjects.Count; i++)
        {
            ObjectData obj = sceneObjects[i];
            GPUObject gObj = new();

            if (obj is SphereData s)
            {
                gObj.type = (int)ObjectType.Sphere;
                gObj.materialIndex = s.materialIndex;

                Transformation T = transformations[s.transformationIndex];
                gObj.center = T.translation; // Sphere unitária transformada
                gObj.radius = 0.5f * Mathf.Max(T.scale.x, Mathf.Max(T.scale.y, T.scale.z));

                gObj.matWorld = T.GetMatrix();
                gObj.invMatWorld = T.GetInverseMatrix();
                gObj.invTranspMatWorld = T.GetInverseTransposeMatrix();
            }
            else if (obj is TrianglePrimitive t)
            {
                gObj.type = (int)ObjectType.Triangle;
                gObj.materialIndex = t.materialIndex;

                Transformation T = transformations[t.transformationIndex];
                gObj.v0 = T.GetMatrix().MultiplyPoint(t.v1);
                gObj.v1 = T.GetMatrix().MultiplyPoint(t.v2);
                gObj.v2 = T.GetMatrix().MultiplyPoint(t.v3);

                gObj.matWorld = T.GetMatrix();
                gObj.invMatWorld = T.GetInverseMatrix();
                gObj.invTranspMatWorld = T.GetInverseTransposeMatrix();
            }
            else if (obj is BoxData b)
            {
                gObj.type = (int)ObjectType.Box;
                gObj.materialIndex = b.materialIndex;

                Transformation T = transformations[b.transformationIndex];

                Matrix4x4 M = T.GetMatrix();
                Matrix4x4 Minv = T.GetInverseMatrix();
                Matrix4x4 MinvT = T.GetInverseTransposeMatrix();

                gObj.matWorld = M;
                gObj.invMatWorld = Minv;
                gObj.invTranspMatWorld = MinvT;

                gObj.min = b.min;
                gObj.max = b.max;

                gObj.v0 = Vector3.zero; gObj.v1 = Vector3.zero; gObj.v2 = Vector3.zero;
                gObj.center = M.MultiplyPoint(Vector3.zero);
                gObj.radius = 0f;
            }

            gpuObjects[i] = gObj;
        }

        return gpuObjects;
    }

    private GPUMaterial[] ConvertMaterialsToGPU()
    {
        GPUMaterial[] gpuMaterials = new GPUMaterial[materials.Count];
        for (int i = 0; i < materials.Count; i++)
        {
            MaterialProperties m = materials[i];
            gpuMaterials[i] = new GPUMaterial
            {
                color = new Vector3(m.color.r, m.color.g, m.color.b),
                ambient = hasAmbient ? m.ambient : 0f,
                diffuse = hasDiffuse ? m.diffuse : 0f,
                specular = hasSpecular ? m.specular : 0f,
                refraction = hasRefraction ? m.refraction : 0f,
                refractionIndex = m.refractionIndex
            };
        }
        return gpuMaterials;
    }

    private GPULight[] ConvertLightsToGPU()
    {
        GPULight[] gpuLights = new GPULight[lights.Count];
        for (int i = 0; i < lights.Count; i++)
        {
            LightData l = lights[i];
            Transformation T = transformations[l.transformationIndex];

            gpuLights[i] = new GPULight
            {
                position = T.translation,
                color = new Vector3(l.color.r, l.color.g, l.color.b)
            };
        }
        return gpuLights;
    }

    public void RenderGPU()
    {
        GPUObject[] gpuObjects = ConvertObjectsToGPU();
        GPUMaterial[] gpuMaterials = ConvertMaterialsToGPU();
        GPULight[] gpuLights = ConvertLightsToGPU();

        if (gpuRayTracer == null)
        {
            Debug.LogError("GPUPrimaryRays component not found! Please add it to the same GameObject.");
            return;
        }

        gpuRayTracer.gpuObjects = gpuObjects;
        gpuRayTracer.gpuMaterials = gpuMaterials;
        gpuRayTracer.gpuLights = gpuLights;

        BVHBuilder bvh = new();
        bvh.GatherPrimitives(sceneObjects, transformations);
        bvh.BuildRecursive();

        this.bvhBuilder = bvh;

        if (bvhBuilder != null)
        {
            bvhBuilder.PackForGPU(
                out Vector3[] nodeMins, out Vector3[] nodeMaxs, out int[] nodeLeft, out int[] nodeRight, out int[] nodeFirstPrim, out int[] nodePrimCount,
                out Vector3[] outPrimMin, out Vector3[] outPrimMax, out int[] outPrimType, out int[] outPrimObjIndex,
                out Vector3[] outTriV0, out Vector3[] outTriV1, out Vector3[] outTriV2, out Vector4[] outSphereCenterRadius);

            gpuRayTracer.bvhNodeMins = nodeMins;
            gpuRayTracer.bvhNodeMaxs = nodeMaxs;
            gpuRayTracer.bvhNodeLeft = nodeLeft;
            gpuRayTracer.bvhNodeRight = nodeRight;
            gpuRayTracer.bvhNodeFirstPrim = nodeFirstPrim;
            gpuRayTracer.bvhNodePrimCount = nodePrimCount;

            gpuRayTracer.primObjIndex = outPrimObjIndex;
            gpuRayTracer.primMin = outPrimMin;
            gpuRayTracer.primMax = outPrimMax;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        gpuRayTracer.Render(imgSettings, rec);
        sw.Stop();
        Debug.Log("Compute time with: " + sw.ElapsedMilliseconds + " ms");
        elapsedTimeMS = sw.ElapsedMilliseconds;

        if (outputImage != null)
            outputImage.texture = gpuRayTracer.outputTexture;
    }

    public void RenderCPU()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        cpuRayTracer = new PrimaryRays(sceneObjects, lights, transformations, materials, imgSettings, cameraData);
        Texture2D output = cpuRayTracer.Render();
        sw.Stop();
        Debug.Log("Compute time with: " + sw.ElapsedMilliseconds + " ms");
        elapsedTimeMS = sw.ElapsedMilliseconds;
        if (outputImage != null)
            outputImage.texture = output;
    }

public void SaveSceneToJson(string path)
    {
        SceneData data = new()
        {
            objects = new List<SerializableObject>(),
            lights = new List<LightData>(),
            transformations = transformations,
            materials = materials
        };

        foreach (var obj in sceneObjects)
        {
            SerializableObject so = new();

            if (obj is TrianglePrimitive t)
            {
                so.type = "triangle";
                so.triangle = t;
            }
            else if (obj is SphereData s)
            {
                so.type = "sphere";
                so.sphere = s;
            }
            else if (obj is BoxData b)
            {
                so.type = "box";
                so.box = b;
            }
            else if (obj is CameraData c)
            {
                so.type = "camera";
                so.camera = c;
            }
            else if (obj is ImageSettings img)
            {
                so.type = "image";
                so.image = img;
            }

            data.objects.Add(so);
        }

        foreach (var light in lights)
            data.lights.Add(light);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log("Scene saved to: " + path);
    }

    public void LoadSceneFromPath(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Scene file not found: " + path);
            return;
        }

        ClearSceneObjects();

        sceneObjects.Clear();
        lights.Clear();
        transformations.Clear();
        materials.Clear();

        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".json")
        {
            string json = File.ReadAllText(path);
            SceneData jsonData = JsonUtility.FromJson<SceneData>(json);

            transformations = jsonData.transformations;
            materials = jsonData.materials;

            foreach (var so in jsonData.objects)
            {
                switch (so.type)
                {
                    case "triangle": sceneObjects.Add(so.triangle); break;
                    case "sphere": sceneObjects.Add(so.sphere); break;
                    case "box": sceneObjects.Add(so.box); break;
                    case "camera": sceneObjects.Add(so.camera); break;
                    case "image": sceneObjects.Add(so.image); break;
                }
            }

            foreach (var l in jsonData.lights)
                lights.Add(l);
        }
        else if (ext == ".txt")
        {
            string txt = File.ReadAllText(path);
            sceneService.LoadScene(
                txt,
                out sceneObjects,
                out lights,
                out transformations,
                out materials
            );
        }
        else
        {
            Debug.LogError("Unsupported scene format: " + ext);
            return;
        }

        BuildScene();
        OnSceneLoaded?.Invoke();
    }

    private void ClearSceneObjects()
    {
        foreach (var go in instantiatedObjects)
        {
            if (go != null)
                Destroy(go);
        }
        instantiatedObjects.Clear();
    }

    // UI methods
    public void SaveCurrentImage(string path) { gpuRayTracer.SaveRenderTextureToPNG(path); }

    // Getters
    public float GetElapsedTime() { return elapsedTimeMS; }
    public int GetRecursionDepth() { return rec; }

    // Image methods
    public int GetImageSettingsW() { return imgSettings.size.x; }
    public int GetImageSettingsY() { return imgSettings.size.y; }

    public float GetBackgroundColorR() { return imgSettings.backgroundColor.r; }
    public float GetBackgroundColorG() { return imgSettings.backgroundColor.g; }
    public float GetBackgroundColorB() { return imgSettings.backgroundColor.b; }

    // Light methods
    public bool GetAmbient() { return hasAmbient; }
    public bool GetDiffuse() { return hasDiffuse; }
    public bool GetSpecular() { return hasSpecular; }
    public bool GetRefraction() { return hasRefraction; }

    // Camera methods
    public float GetCameraFOV() { return cameraData.fov; }

    public float GetCameraPositionX() { return transformations[cameraData.transformationIndex].translation.x; }
    public float GetCameraPositionY() { return transformations[cameraData.transformationIndex].translation.y; }
    public float GetCameraPositionZ() { return transformations[cameraData.transformationIndex].translation.z; }

    public float GetCameraRotationX() { return transformations[cameraData.transformationIndex].rotation.x; }
    public float GetCameraRotationY() { return transformations[cameraData.transformationIndex].rotation.y; }
    public float GetCameraRotationZ() { return transformations[cameraData.transformationIndex].rotation.z; }

    // Setters
    public void SetRecursionDepth(int depth) { rec = depth; }

    // Image methods
    public void SetImageWidth(int w) { imgSettings.size.x = Mathf.Max(8, w); }
    public void SetImageHeight(int h)
    {
        imgSettings.size.y = Mathf.Max(8, h);
    }

    public void SetBackgroundColorR(int r) { imgSettings.backgroundColor.r = Mathf.Clamp01(r / 255f); }
    public void SetBackgroundColorG(int g) { imgSettings.backgroundColor.g = Mathf.Clamp01(g / 255f); }
    public void SetBackgroundColorB(int b) { imgSettings.backgroundColor.b = Mathf.Clamp01(b / 255f); }

    // Light methods
    public void SetAmbient(bool value) 
    { 
        foreach (MaterialProperties mat in materials)
        {
            foreach (var obj in sceneObjects)
            {
                int mIndex = obj switch
                {
                    SphereData s => s.materialIndex,
                    BoxData b => b.materialIndex,
                    TrianglePrimitive t => t.materialIndex,
                    _ => -1
                };
                if (mIndex >= 0 && mIndex < materials.Count)
                {
                    if (value)
                        ApplyMaterial(instantiatedObjects[mIndex], mat);
                    else 
                        ApplyMaterial(instantiatedObjects[mIndex], new MaterialProperties(mat.color.r, mat.color.g, mat.color.b, 0f, mat.diffuse, mat.specular, mat.refraction, mat.refractionIndex));
                }
            }
        }
        hasAmbient = value;
    }
    public void SetDiffuse(bool value)
    {
        foreach (MaterialProperties mat in materials)
        {
            foreach (var obj in sceneObjects)
            {
                int mIndex = obj switch
                {
                    SphereData s => s.materialIndex,
                    BoxData b => b.materialIndex,
                    TrianglePrimitive t => t.materialIndex,
                    _ => -1
                };
                if (mIndex >= 0 && mIndex < materials.Count)
                {
                    if (value)
                        ApplyMaterial(instantiatedObjects[mIndex], mat);
                    else
                        ApplyMaterial(instantiatedObjects[mIndex], new MaterialProperties(mat.color.r, mat.color.g, mat.color.b, mat.ambient, 0f, mat.specular, mat.refraction, mat.refractionIndex));
                }
            }
        }
        hasDiffuse = value;
    }
    public void SetSpecular(bool value)
    {
        foreach (MaterialProperties mat in materials)
        {
            foreach (var obj in sceneObjects)
            {
                int mIndex = obj switch
                {
                    SphereData s => s.materialIndex,
                    BoxData b => b.materialIndex,
                    TrianglePrimitive t => t.materialIndex,
                    _ => -1
                };
                if (mIndex >= 0 && mIndex < materials.Count)
                {
                    if (value)
                        ApplyMaterial(instantiatedObjects[mIndex], mat);
                    else
                        ApplyMaterial(instantiatedObjects[mIndex], new MaterialProperties(mat.color.r, mat.color.g, mat.color.b, mat.ambient, mat.diffuse, 0f, mat.refraction, mat.refractionIndex));
                }
            }
        }
        hasSpecular = value;
    }
    public void SetRefraction(bool value)
    {
        foreach (MaterialProperties mat in materials)
        {
            foreach (var obj in sceneObjects)
            {
                int mIndex = obj switch
                {
                    SphereData s => s.materialIndex,
                    BoxData b => b.materialIndex,
                    TrianglePrimitive t => t.materialIndex,
                    _ => -1
                };
                if (mIndex >= 0 && mIndex < materials.Count)
                {
                    if (value)
                        ApplyMaterial(instantiatedObjects[mIndex], mat);
                    else
                        ApplyMaterial(instantiatedObjects[mIndex], new MaterialProperties(mat.color.r, mat.color.g, mat.color.b, mat.ambient, mat.diffuse, mat.specular, 0f, mat.refractionIndex));
                }
            }
        }
        hasRefraction = value;
    }

    // Camera methods
    public void SetCameraFOV(float fov) { cameraData.fov = fov; cameraGO.fieldOfView = fov; }

    public void SetCameraPositionX(float x) { transformations[cameraData.transformationIndex].translation.x = x; ReapplyCameraTransform(); }
    public void SetCameraPositionY(float y) { transformations[cameraData.transformationIndex].translation.y = y; ReapplyCameraTransform(); }
    public void SetCameraPositionZ(float z) { transformations[cameraData.transformationIndex].translation.z = z; ReapplyCameraTransform(); }
    public void SetCameraRotationX(float x) { transformations[cameraData.transformationIndex].rotation.x = x; ReapplyCameraTransform(); }
    public void SetCameraRotationY(float y) { transformations[cameraData.transformationIndex].rotation.y = y; ReapplyCameraTransform(); }
    public void SetCameraRotationZ(float z) { transformations[cameraData.transformationIndex].rotation.z = z; ReapplyCameraTransform(); }

    // Helpers
    private void ReapplyCameraTransform()
    {
        var t = transformations[cameraData.transformationIndex];

        Vector3 basePos = t.translation;

        Vector3 distanceOffset = new(0, 0, cameraData.distance);

        cameraGO.transform.position = basePos + distanceOffset;
        cameraGO.transform.rotation = Quaternion.Euler(t.rotation);
    }
}