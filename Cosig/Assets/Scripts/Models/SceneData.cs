using System.Collections.Generic;
using UnityEngine;
using Models;

[System.Serializable]
public class SceneData
{
    public List<SerializableObject> objects;
    public List<LightData> lights;
    public List<Transformation> transformations;
    public List<MaterialProperties> materials;
}
