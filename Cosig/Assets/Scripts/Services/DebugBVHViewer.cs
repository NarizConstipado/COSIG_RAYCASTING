using UnityEngine;
using System.Collections.Generic;

public class DebugBVHViewer : MonoBehaviour
{
    public BVHBuilder bvh;
    public Color colorSphere = Color.red;
    public Color colorTriangle = Color.green;
    public Color colorBox = Color.blue;
    public float lineWidth = 0.02f;

    void OnDrawGizmos()
    {
        if (bvh == null || bvh.prims == null) return;

        for (int i = 0; i < bvh.prims.Count; i++)
        {
            var p = bvh.prims[i];

            if (p.primType == 0) Gizmos.color = colorSphere;
            else if (p.primType == 1) Gizmos.color = colorTriangle;
            else if (p.primType == 2) Gizmos.color = colorBox;

            DrawAABB(p.min, p.max);
        }
    }

    void DrawAABB(Vector3 min, Vector3 max)
    {
        Vector3 p000 = new Vector3(min.x, min.y, min.z);
        Vector3 p001 = new Vector3(min.x, min.y, max.z);
        Vector3 p010 = new Vector3(min.x, max.y, min.z);
        Vector3 p011 = new Vector3(min.x, max.y, max.z);
        Vector3 p100 = new Vector3(max.x, min.y, min.z);
        Vector3 p101 = new Vector3(max.x, min.y, max.z);
        Vector3 p110 = new Vector3(max.x, max.y, min.z);
        Vector3 p111 = new Vector3(max.x, max.y, max.z);

        Gizmos.DrawLine(p000, p001);
        Gizmos.DrawLine(p001, p011);
        Gizmos.DrawLine(p011, p010);
        Gizmos.DrawLine(p010, p000);

        Gizmos.DrawLine(p100, p101);
        Gizmos.DrawLine(p101, p111);
        Gizmos.DrawLine(p111, p110);
        Gizmos.DrawLine(p110, p100);

        Gizmos.DrawLine(p000, p100);
        Gizmos.DrawLine(p001, p101);
        Gizmos.DrawLine(p010, p110);
        Gizmos.DrawLine(p011, p111);
    }
}
