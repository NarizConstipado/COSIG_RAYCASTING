using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Models
{
    [System.Serializable]
    public class ImageSettings : ObjectData
    {
        public Vector2Int size;
        public Color backgroundColor;

        public ImageSettings(int resX, int resY, float colorR, float colorG, float colorB)
        {
            size = new Vector2Int(resX, resY);
            backgroundColor = new Color(colorR, colorG, colorB);
        }
    }

    [System.Serializable]
    public class CameraData : ObjectData
    {
        public int transformationIndex;
        public float distance;
        public float fov;

        public CameraData(int tIndex, float dist, float fovDegree)
        {
            transformationIndex = tIndex;
            distance = dist;
            fov = fovDegree;
        }
        
    }

    [System.Serializable]
    public class LightData : ObjectData
    {
        public int transformationIndex;
        public Color color;

        public LightData(int tIndex, float r, float g, float b)
        {
            transformationIndex = tIndex;
            color = new Color((float)r, (float)g, (float)b);
        }
    }

    [System.Serializable]
    public class TrianglePrimitive : ObjectData
    {
        public int transformationIndex;
        public int materialIndex;
        public Vector3 v1, v2, v3;

        public TrianglePrimitive(int tIndex, int mIndex, float v1x, float v1y, float v1z,float v2x, float v2y, float v2z,float v3x, float v3y, float v3z)
        {
            transformationIndex = tIndex;
            materialIndex = mIndex;
            v1 = new Vector3(v1x, v1y, v1z);
            v2 = new Vector3(v2x, v2y, v2z);
            v3 = new Vector3(v3x, v3y, v3z);
        }

        /*
        public Vector3 ComputeNormal()
        {
            Vector3 edgeAB = v1 - v0;
            Vector3 edgeAC = v2 - v0;
            Vector3 n = Vector3.Cross(edgeAB, edgeAC);
            if (n.sqrMagnitude <= Mathf.Epsilon) return Vector3.up;
            return n.normalized;
        }
        */
    }

    [System.Serializable]
    public class SphereData : ObjectData
    {
        public int transformationIndex;
        public int materialIndex;

        public SphereData(int tIndex, int mIndex)
        {
            transformationIndex = tIndex;
            materialIndex = mIndex;
        }
    }

    [System.Serializable]
    public class BoxData : ObjectData
    {
        public int transformationIndex;
        public int materialIndex;

        public BoxData(int tIndex, int mIndex)
        {
            transformationIndex = tIndex;
            materialIndex = mIndex;
        }
    }

    [System.Serializable]
    public abstract class ObjectData
    {
        
    }
}
