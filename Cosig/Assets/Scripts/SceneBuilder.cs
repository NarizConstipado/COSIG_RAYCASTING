using System.Collections.Generic;
using UnityEngine;
using Models;
using Services;
using System.IO;
using UnityEngine.UI;

public class SceneBuilder : MonoBehaviour
{
    public Material baseMaterial;

    private SceneService sceneService = new SceneService();

    private SceneData sceneData = new SceneData();

    private List<ObjectData> sceneObjects = new List<ObjectData>();
    private List<LightData> lights = new List<LightData>();
    private List<Transformation> transformations = new List<Transformation>();
    private List<MaterialProperties> materials = new List<MaterialProperties>();

    public RawImage outputImage;

    // Guardar o BVHBuilder como campo para poder empacotar para GPU mais tarde
    private BVHBuilder bvhBuilder;

    void Start()
    {
        string resourceName = null;// "Config/Test Scene 1";
        string jsonPath = Application.dataPath + "/Scripts/Resources/Config/Test Scene 1_js.json";

        if (File.Exists(jsonPath))
        {
            string jsonFile = File.ReadAllText(jsonPath);
            SceneData jsonData = JsonUtility.FromJson<SceneData>(jsonFile);

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
                    default:
                        Debug.LogWarning("Unknown object type in JSON: " + so.type);
                        break;
                }
            }
            foreach (var light in jsonData.lights)
            {
                lights.Add(light);
            }

            BuildScene();
            return;
        }

        TextAsset txtFile = Resources.Load<TextAsset>(resourceName);

        if (txtFile == null)
        {
            Debug.LogError("ERROR: Cannot load TXT or JSON at: " + resourceName);
            return;
        }
        sceneService.LoadScene(txtFile.text,
            out sceneObjects,
            out lights,
            out transformations,
            out materials);

        SceneData data = new SceneData();
        data.objects = new List<SerializableObject>();
        data.lights = new List<LightData>();
        data.transformations = transformations;
        data.materials = materials;

        foreach (var obj in sceneObjects)
        {
            SerializableObject so = new SerializableObject();

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
        {
            LightData lightData = light;
            data.lights.Add(light);
        }

        string jsonStringFile = JsonUtility.ToJson(data, true);
        File.WriteAllText(jsonPath, jsonStringFile);

        Debug.Log("TXT converted to JSON: " + jsonPath);

        BuildScene();
    }
    // Method para criar cada objeto na cena
    void BuildScene()
    {
        ImageSettings img = null;
        CameraData cam = null;

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

                Mesh mesh = new Mesh();
                mf.mesh = mesh;

                Vector3[] vertices = new Vector3[]
                {
                    triData.v1,
                    triData.v2,
                    triData.v3
                };

                Vector2[] uvs = new Vector2[]
                {
                        new Vector2(0, 0),
                        new Vector2(0, 1),
                        new Vector2(1, 1)
                };


                int[] triangles = new int[] {0, 1, 2};
                Vector3 normal = Vector3.Cross(triData.v2 - triData.v1, triData.v3 - triData.v1).normalized;
                Vector3[] normals = new Vector3[] {normal, normal, normal};
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
                var camera = camObj.AddComponent<Camera>();
                camera.fieldOfView = camData.fov;
                camObj.transform.position = new Vector3(0, 0, camData.distance);
                camObj.tag = "MainCamera";
                obj = camObj;
                cam = camData;
            }

            else if (objData is ImageSettings imgData) img = imgData;


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
        }
        foreach(var lightData in lights)
        {
            GameObject obj = null;
            var lightObj = new GameObject("Light");
            var light = lightObj.AddComponent<Light>();
            light.color = lightData.color;
            obj = lightObj;
            ApplyTransformation(lightObj, transformations[lightData.transformationIndex]);
        }

        // --- Render CPU ---
        //PrimaryRays tracer = new PrimaryRays(sceneObjects, lights, transformations, materials, img, cam);

        //Texture2D tex = tracer.Render();

        //outputImage.texture = tex;
        //byte[] png = tex.EncodeToPNG();
        //System.IO.File.WriteAllBytes(Application.dataPath + "/RayTraceOutput.png", png);

        // --- Build BVH ---
        BVHBuilder bvh = new BVHBuilder();
        bvh.GatherPrimitives(sceneObjects, transformations);
        bvh.BuildRecursive();

        // guardar BVHBuilder para enviar ao GPU
        this.bvhBuilder = bvh;

        // Debugger para visualizar a BVH na cena
        var viewerGO = new GameObject("BVH Debug Viewer");
        var viewer = viewerGO.AddComponent<DebugBVHViewer>();
        viewer.bvh = bvh;

        RenderGPU(img);
    }

    void ApplyTransformation(GameObject obj, Transformation transformation)
    {
        if (transformation == null) return;
        obj.transform.Translate(transformation.translation, Space.World);
        obj.transform.Rotate(transformation.rotation);
        obj.transform.localScale = transformation.scale;
    }

    void ApplyMaterial(GameObject obj, MaterialProperties properties)
    {
        Material newMaterial = new Material(baseMaterial);
        newMaterial.color = properties.color;

        //newMaterial.SetFloat("_Metallic", (float)properties.specular);
        //newMaterial.SetFloat("_Glossiness", (float)properties.diffuse);

        newMaterial.SetFloat("_Ambient", properties.ambient);
        newMaterial.SetFloat("_Diffuse", properties.diffuse);
        newMaterial.SetFloat("_Specular", properties.specular);
        newMaterial.SetFloat("_Refract", properties.refraction);
        newMaterial.SetFloat("_RefractIndex", properties.refractionIndex);

        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.material = newMaterial;
    }

    private GPUObject[] ConvertObjectsToGPU()
    {
        GPUObject[] gpuObjects = new GPUObject[sceneObjects.Count];

        for (int i = 0; i < sceneObjects.Count; i++)
        {
            ObjectData obj = sceneObjects[i];
            GPUObject gObj = new GPUObject();

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
                ambient = m.ambient,
                diffuse = m.diffuse,
                specular = m.specular
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

    private void RenderGPU(ImageSettings imgSettings)
    {
        GPUObject[] gpuObjects = ConvertObjectsToGPU();
        GPUMaterial[] gpuMaterials = ConvertMaterialsToGPU();
        GPULight[] gpuLights = ConvertLightsToGPU();

        GPUPrimaryRays gpuRayTracer = GetComponent<GPUPrimaryRays>();
        if (gpuRayTracer == null)
        {
            Debug.LogError("GPUPrimaryRays component not found! Please add it to the same GameObject.");
            return;
        }

        gpuRayTracer.gpuObjects = gpuObjects;
        gpuRayTracer.gpuMaterials = gpuMaterials;
        gpuRayTracer.gpuLights = gpuLights;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        gpuRayTracer.Render(imgSettings);
        sw.Stop();
        Debug.Log("Compute time without: " + sw.ElapsedMilliseconds + " ms");

        // Pack BVH and set arrays on gpuRayTracer
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
            // primMin/primMax not required by shader traversal since shader will look up object via primObjIndex,
            // mas deixo-os disponíveis caso queira expandir.
            gpuRayTracer.primMin = outPrimMin;
            gpuRayTracer.primMax = outPrimMax;
        }

        sw = System.Diagnostics.Stopwatch.StartNew();
        gpuRayTracer.Render(imgSettings);
        sw.Stop();
        Debug.Log("Compute time with: " + sw.ElapsedMilliseconds + " ms");

        if (outputImage != null)
            outputImage.texture = gpuRayTracer.outputTexture;
    }
}