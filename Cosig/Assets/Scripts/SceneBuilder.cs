// SceneBuilder.cs
using System.Collections.Generic;
using UnityEngine;
using Models;
using Services;

// Unity component responsible for constructing and rendering the scene based on loaded data
public class SceneBuilder : MonoBehaviour
{
    public Material baseMaterial; // Base material used as a template for object materials
    private SceneService sceneService = new SceneService(); // Service instance to load scene data

    private List<ObjectData> sceneObjects = new List<ObjectData>(); // List of scene objects
    private List<Transformation> transformations = new List<Transformation>();
    private List<MaterialProperties> materials = new List<MaterialProperties>();
    void Start()
    {
        // Durante debugging usa o caminho absoluto ou um TextAsset em Resources.
        // Exemplo de Resource: "Config/Test Scene 1" (sem .txt) se o ficheiro estiver em Assets/Resources/Config/
        string filePath = "Config/Test Scene 1";
        sceneService.LoadScene(filePath, out sceneObjects, out transformations, out materials); // Load objects from configuration
        foreach (var obj in sceneObjects) obj.DebugSummary();
        BuildScene(); // Build and display the scene
    }
    // Method to create each object in the scene based on loaded data
    void BuildScene()
    {
        foreach (var objData in sceneObjects)
        {
            if (objData is SphereData) GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            if (objData is BoxData) GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (objData is TrianglePrimitive triData)
            {
                //criar triângulo
            }

            if (objData is CameraData camData)
            {
                var camObj = new GameObject("Camera");
                var camera = camObj.AddComponent<Camera>();
                camera.fieldOfView = camData.fov;
                camObj.AddComponent<Camera>();
                camObj.transform.position = new Vector3(0, 0, camData.distance);
                GameObject obj = camObj;
            }

            if (objData is LightData lightData)
            {
                var lightObj = new GameObject("Light");
                var light = lightObj.AddComponent<Light>();
                light.color = lightData.color;
                GameObject obj = lightObj;
            }

            // Aplica cada transforma��o (se houverem)
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
                ;
                ApplyTransformation(obj, transformations[tIndex]);
            }
            
            int mIndex = objData switch
            {
                SphereData s => s.materialIndex,
                BoxData b => b.materialIndex,
                TrianglePrimitive t => t.materialIndex,
                _ => -1
            };

            if (mIndex >= 0 && mIndex < materials.Count)
            {;
                ApplyMaterial(obj, materials[mIndex]);
            }
        }
    }
    // Apply transformations to a given object based on the list of transformations
    void ApplyTransformation(GameObject obj, Transformation transformation)
    {
        if (transformation == null) return;
        obj.transform.Translate(trans.translation, Space.World); // Apply position
        obj.transform.Rotate(trans.rotation); // Apply rotation
        obj.transform.localScale = trans.scale; // Apply scale
    }
    // Apply material properties to the given object
    void ApplyMaterial(GameObject obj, MaterialProperties properties)
    {
        if (baseMaterial == null || properties == null) return;

        Material newMaterial = new Material(baseMaterial); // Create new material from base
        newMaterial.color = properties.color; // Set color
        //newMaterial.SetFloat("_Shininess", properties.shininess); // Set shininess
        //newMaterial.SetFloat("_Metallic", properties.metallic); // Set metallic
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.material = newMaterial; // Assign material to object
    }
}