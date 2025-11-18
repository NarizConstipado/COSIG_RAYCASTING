using UnityEngine;

[System.Serializable]
public class Transformation
{
    public Vector3 translation;
    public Vector3 rotation;
    public Vector3 scale;

    public Transformation(float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float scaleX, float scaleY, float scaleZ)
    {
        translation = new Vector3(posX, posY, posZ);
        rotation = new Vector3(rotX, rotY, rotZ);
        scale = new Vector3(scaleX, scaleY, scaleZ);
    }
}
