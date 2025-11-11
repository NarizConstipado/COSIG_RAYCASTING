using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Models
{
    [System.Serializable]
    public class ImageSettings(int resX, int resY, double colorR, double colorG, double colorB)
    {
        public Vector2Int size = new Vector2Int(resX, resY);
        public Color backgroundColor = new Color(colorR, colorG, colorB);
    }

    [System.Serializable]
    public class CameraData(int t_index, double distance, double fovDegrees)
    {
        public int index = t_index;
        public float fov = fovDegrees;
        public float distance = distance;
    }

    [System.Serializable]
    public class LightData(int t_index, double colorR, double colorG, double colorB)
    {
        public int index = t_index;
        public Color color = new Color(colorR, colorG, colorB);
    }

    [System.Serializable]
    public class MaterialProperties(double colorR, double colorG, double colorB, double amb, double dif, double spec, double refr, double refrI)
    {
        public Color color = new Color(colorR, colorG, colorB);
        public float ambient = amb;
        public float diffuse = dif;
        public float specular = spec;
        public float refraction = refr;
        public float refractionIndex = refrI;
    }

    [System.Serializable]
    public class Transformation(double posX, double posY, double posZ, double rotX, double rotY, double rotZ, double scaleX, double scaleY, double scaleZ)
    {
        public Vector3 translation = new Vector3(posX, posY, posZ);
        public Vector3 rotation = new Vector3(rotX, rotY, rotZ);
        public Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);
    }

    [System.Serializable]
    public class TrianglePrimitive(int t_index, int m_index, double v0x, double v0y, double v0z, double v1x, double v1y, double v1z, double v2x, double v2y, double v2z)
    {
        public Vector3 = new Vector3(v0x, v0y, v0z);
        public Vector3 = new Vector3(v1x, v1y, v1z);
        public Vector3 = new Vector3(v2x, v2y, v2z);
        public int transformationIndex = t_index;
        public int materialIndex = m_index;

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
    public class SphereData(int t_index, int m_index)
    {
        public int transformationIndex = t_index;
        public int materialIndex = m_index;
    }

    [System.Serializable]
    public class BoxData(int t_index, int m_index)
    {
        public int transformationIndex = t_index;
        public int materialIndex = m_index;
    }

    [System.Serializable]
    
    /*public class ObjectData
    {
        public ImageSettings image = new ImageSettings();
        public List<Transformation> transformations = new();
        public List<MaterialProperties> materials = new();
        public CameraData camera = new CameraData();
        public List<LightData> lights = new();
        public List<TrianglePrimitive> triangles = new();
        public List<SphereData> spheres = new();
        public List<BoxData> boxes = new();

        public void DebugSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ObjectData Summary ===");
            sb.AppendLine($"Image: {image.size.x}x{image.size.y}, BG={image.backgroundColor}");
            sb.AppendLine($"Transformations: {transformations.Count}");
            for (int i = 0; i < transformations.Count; i++)
            {
                var t = transformations[i];
                sb.AppendLine($" T[{i}] pos={t.translation} rot={t.rotation} scale={t.scale}");
            }
            sb.AppendLine($"Materials: {materials.Count}");
            for (int i = 0; i < materials.Count; i++)
            {
                var m = materials[i];
                sb.AppendLine($" M[{i}] color={m.color} shininess={m.shininess} metallic={m.metallic}");
            }
            sb.AppendLine($"Camera: idx={camera.index} fov={camera.fov} transformIdx={camera.transformationIndex}");
            sb.AppendLine($"Lights: {lights.Count}");
            for (int i = 0; i < lights.Count; i++)
            {
                var L = lights[i];
                sb.AppendLine($" L[{i}] pos={L.position} color={L.color} intensity={L.intensity} transformIdx={L.transformationIndex}");
            }
            sb.AppendLine($"Triangles: {triangles.Count}");
            if (triangles.Count > 0)
            {
                var n = triangles[0].ComputeNormal();
                sb.AppendLine($" Example triangle normal (first): {n}");
            }
            sb.AppendLine($"Spheres: {spheres.Count}, Boxes: {boxes.Count}");
            sb.AppendLine("=== End Summary ===");
            Debug.Log(sb.ToString());
        }
    }*/
}
