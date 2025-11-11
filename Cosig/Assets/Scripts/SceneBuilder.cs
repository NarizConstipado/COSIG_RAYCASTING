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
    void Start()
    {
        // Durante debugging usa o caminho absoluto ou um TextAsset em Resources.
        // Exemplo de Resource: "Config/Test Scene 1" (sem .txt) se o ficheiro estiver em Assets/Resources/Config/
        string filePath = "Assets/Scripts/Resources/Config/Test Scene 1.txt";
        sceneObjects = sceneService.LoadSceneObjects(filePath); // Load objects from configuration
        foreach (var obj in sceneObjects) obj.DebugSummary();
        BuildScene(); // Build and display the scene
    }
    // Method to create each object in the scene based on loaded data
    void BuildScene()
    {
        foreach (var objData in sceneObjects)
        {
            // Create a primitive object (using a cube as an example here)
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Aplica cada transformação (se houverem)
            ApplyTransformations(obj, objData.transformations);

            // Seleciona um MaterialProperties válido (usa o primeiro material lido, se existir)
            MaterialProperties matProps = (objData.materials != null && objData.materials.Count > 0)
                ? objData.materials[0]
                : new MaterialProperties();

            ApplyMaterial(obj, matProps); // Apply material properties to object
        }
    }
    // Apply transformations to a given object based on the list of transformations
    void ApplyTransformations(GameObject obj, List<Transformation> transformations)
    {
        if (transformations == null) return;
        foreach (var trans in transformations)
        {
            obj.transform.Translate(trans.translation, Space.World); // Apply position
            obj.transform.Rotate(trans.rotation); // Apply rotation
            obj.transform.localScale = trans.scale; // Apply scale
        }
    }
    // Apply material properties to the given object
    void ApplyMaterial(GameObject obj, MaterialProperties properties)
    {
        if (baseMaterial == null || properties == null) return;
        Material newMaterial = new Material(baseMaterial); // Create new material from base
        newMaterial.color = properties.color; // Set color
        newMaterial.SetFloat("_Shininess", properties.shininess); // Set shininess
        newMaterial.SetFloat("_Metallic", properties.metallic); // Set metallic
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.material = newMaterial; // Assign material to object
    }
}