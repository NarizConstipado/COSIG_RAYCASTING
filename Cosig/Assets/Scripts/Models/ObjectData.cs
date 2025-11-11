using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneData
{
    public Vector2Int imageSize;
    public Color backgroundColor;
    public List<Transformation> transformations = new();
    public List<MaterialProperties> materials = new();

    public void DebugSummary()
    {
        Debug.Log($"Scene Loaded: {transformations.Count} transforms, {materials.Count} materials");
    }
}
