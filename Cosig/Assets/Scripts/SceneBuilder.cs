using System.Collections.Generic;
using UnityEngine;
using Models;
using Services;
public class SceneBuilder : MonoBehaviour
{
    private SceneService sceneService = new SceneService();

    private List<ObjectData> sceneObjects = new List<ObjectData>();
    private List<Transformation> transformations = new List<Transformation>();
    private List<MaterialProperties> materials = new List<MaterialProperties>();
    void Start()
    {
        // Durante debugging usa o caminho absoluto ou um TextAsset em Resources.
        // Exemplo de Resource: "Config/Test Scene 1" (sem .txt) se o ficheiro estiver em Assets/Resources/Config/
        TextAsset textFile = Resources.Load<TextAsset>("Config/Test Scene 2");

        if (textFile == null)
        {
            Debug.LogError("Could not load Test Scene 1.txt from Resources/Config/");
            return;
        }

        sceneService.LoadScene(textFile.text, out sceneObjects, out transformations, out materials); // Load objects from configuration
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

            else if (objData is LightData lightData)
            {
                var lightObj = new GameObject("Light");
                var light = lightObj.AddComponent<Light>();
                light.color = lightData.color;
                obj = lightObj;
            }

            else if (objData is ImageSettings imgData) img = imgData;

            // Identifica e aplica transformação
            int tIndex = objData switch
            {
                SphereData s => s.transformationIndex,
                BoxData b => b.transformationIndex,
                TrianglePrimitive t => t.transformationIndex,
                CameraData c => c.transformationIndex,
                LightData l => l.transformationIndex,
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

        PrimaryRays tracer = new PrimaryRays(img, cam);

        Texture2D tex = tracer.Render();
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
        Material newMaterial = new Material(Shader.Find("Standard"));
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