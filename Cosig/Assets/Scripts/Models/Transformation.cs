// Models/Transformation.cs
using UnityEngine;
// Class to represent transformations for an object, including position, rotation, and scale
[System.Serializable]
    public class Transformation(double posX, double posY, double posZ, double rotX, double rotY, double rotZ, double scaleX, double scaleY, double scaleZ)
    {
        public Vector3 translation = new Vector3(posX, posY, posZ);
        public Vector3 rotation = new Vector3(rotX, rotY, rotZ);
        public Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);
    }
