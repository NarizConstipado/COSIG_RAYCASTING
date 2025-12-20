using UnityEngine;
using System.Collections.Generic;

public class DebugBVHViewer : MonoBehaviour
{
    public BVHBuilder bvh;

    [Header("Visual Settings")]
    public float lineWidth = 0.2f;

    public Color sphereColor = Color.red;
    public Color triangleColor = Color.green;
    public Color boxColor = Color.blue;
    public Color nodeColor = Color.yellow;

    private List<GameObject> lines = new List<GameObject>();

    public void Build()
    {
        if (bvh == null) return;

        DrawBVHNodes();
        DrawPrimitives();
    }
    void OnDestroy()
    {
        ClearLines();
    }

    void ClearLines()
    {
        foreach (var l in lines)
            if (l) Destroy(l);
        lines.Clear();
    }

    void DrawBVHNodes()
    {
        foreach (var node in bvh.nodes)
        {
            DrawAABB(node.boundsMin, node.boundsMax, nodeColor);
        }
    }

    void DrawPrimitives()
    {
        foreach (var p in bvh.prims)
        {
            Color c = p.primType switch
            {
                0 => sphereColor,
                1 => triangleColor,
                2 => boxColor,
                _ => Color.white
            };

            DrawAABB(p.min, p.max, c);
        }
    }

    void DrawAABB(Vector3 min, Vector3 max, Color color)
    {
        Vector3 p000 = new(min.x, min.y, min.z);
        Vector3 p001 = new(min.x, min.y, max.z);
        Vector3 p010 = new(min.x, max.y, min.z);
        Vector3 p011 = new(min.x, max.y, max.z);
        Vector3 p100 = new(max.x, min.y, min.z);
        Vector3 p101 = new(max.x, min.y, max.z);
        Vector3 p110 = new(max.x, max.y, min.z);
        Vector3 p111 = new(max.x, max.y, max.z);

        // base
        DrawLine(p000, p001, color);
        DrawLine(p001, p101, color);
        DrawLine(p101, p100, color);
        DrawLine(p100, p000, color);

        // topo
        DrawLine(p010, p011, color);
        DrawLine(p011, p111, color);
        DrawLine(p111, p110, color);
        DrawLine(p110, p010, color);

        // verticais
        DrawLine(p000, p010, color);
        DrawLine(p001, p011, color);
        DrawLine(p100, p110, color);
        DrawLine(p101, p111, color);
    }

    void DrawLine(Vector3 a, Vector3 b, Color color)
    {
        GameObject go = new GameObject("BVH_Line");
        go.transform.parent = transform;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        lr.useWorldSpace = true;

        lines.Add(go);
    }
}
