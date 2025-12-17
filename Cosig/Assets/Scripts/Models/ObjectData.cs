using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Models
{
    public enum ObjectType { Sphere = 0, Triangle = 1, Box = 2 }

    public struct GPUObject
    {
        public int type;           // 0 = Esfera, 1 = Triangulo, 2 = Cubo
        public int materialIndex;

        // Esfera
        public Vector3 center;
        public float radius;

        // Triangulo
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;

        // Cubo
        public Vector3 min;
        public Vector3 max;

        // Transformação
        public Matrix4x4 matWorld;
        public Matrix4x4 invMatWorld;
        public Matrix4x4 invTranspMatWorld;
    }

    public struct GPUMaterial
    {
        public Vector3 color;
        public float ambient;
        public float diffuse;
        public float specular;
        public float refraction;
        public float refractionIndex;
    }

    public struct GPULight
    {
        public Vector3 position;
        public Vector3 color;
    }

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
    public class LightData
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
        public Vector3 min = new Vector3(-0.5f, -0.5f, -0.5f);
        public Vector3 max = new Vector3(0.5f, 0.5f, 0.5f);

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

    [System.Serializable]
    public class SerializableObject
    {
        public string type;
        public TrianglePrimitive triangle;
        public SphereData sphere;
        public BoxData box;
        public CameraData camera;
        public ImageSettings image;
    }
}
