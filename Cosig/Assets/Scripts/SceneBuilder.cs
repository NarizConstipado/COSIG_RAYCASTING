using System.Collections.Generic;
using UnityEngine;
using Models;
using Services;
using System.IO;

public class SceneBuilder : MonoBehaviour
{
    public Material baseMaterial;

    private SceneService sceneService = new SceneService();

    private SceneData sceneData = new SceneData();

    private List<ObjectData> sceneObjects = new List<ObjectData>();
    private List<LightData> lights = new List<LightData>();
    private List<Transformation> transformations = new List<Transformation>();
    private List<MaterialProperties> materials = new List<MaterialProperties>();
    void Start()
    {
        string resourceName = null;//"Config/Test Scene 1";
        string jsonPath = Application.dataPath + "/Scripts/Resources/Config/Test Scene 1_js.json";

        // ---1) If JSON exists → USE JSON ---
        if (File.Exists(jsonPath))
        {
            Debug.Log("Loading JSON scene: " + jsonPath);

            string jsonFile = File.ReadAllText(jsonPath);
            SceneData jsonData = JsonUtility.FromJson<SceneData>(jsonFile);

            // Restore lists for BuildScene()
            transformations = jsonData.transformations;
            materials = jsonData.materials;

            // Rebuild ObjectData list from SerializableObject list
            foreach (var so in jsonData.objects)
            {
                switch (so.type)
                {
                    case "triangle": sceneObjects.Add(so.triangle); break;
                    case "sphere": sceneObjects.Add(so.sphere); break;
                    case "box": sceneObjects.Add(so.box); break;
                    case "camera": sceneObjects.Add(so.camera); break;
                    case "light": lights.Add(so.light); break;
                    case "image": sceneObjects.Add(so.image); break;
                    default:
                        Debug.LogWarning("Unknown object type in JSON: " + so.type);
                        break;
                }
            }

            BuildScene();
            return;
        }

        // --- 2) If NO JSON: load TXT ---
        TextAsset txtFile = Resources.Load<TextAsset>(resourceName);

        if (txtFile == null)
        {
            Debug.LogError("ERROR: Cannot load TXT or JSON at: " + resourceName);
            return;
        }

        Debug.Log("TXT found → converting to JSON...");

        // Parse TXT
        sceneService.LoadScene(txtFile.text,
            out sceneObjects,
            out lights,
            out transformations,
            out materials);

        // Create SceneData
        SceneData data = new SceneData();
        data.objects = new List<SerializableObject>();
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
            SerializableObject so = new SerializableObject();
            so.type = "light";
            so.light = light;
            data.objects.Add(so);
        }

        // Save JSON
        string jsonStringFile = JsonUtility.ToJson(data, true);
        File.WriteAllText(jsonPath, jsonStringFile);

        Debug.Log("TXT converted to JSON: " + jsonPath);

        BuildScene();
    }
    // Method para criar cada objeto na cena
    void BuildScene()
    {
        // Teste
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
                obj = camObj;
                // Teste
                cam = camData;
            }

            else if (objData is ImageSettings imgData) img = imgData;

            // Identifica e aplica transformação
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

        PrimaryRays tracer = new PrimaryRays(sceneObjects, transformations, materials, img, cam);

        Texture2D tex = tracer.Render();
        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/RayTraceOutput.png", png);
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
}