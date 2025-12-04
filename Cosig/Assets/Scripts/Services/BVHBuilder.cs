// Services/BVHBuilder.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Models;

public struct BVHNode
{
    public Vector3 boundsMin;
    public Vector3 boundsMax;
    public int left;
    public int right;
    public int firstPrim;   // índice no array de prims
    public int primCount;   // quantos prims neste nó (leaf)
}

public class BVHBuilder
{
    // We'll represent primitives by bounding box + reference to object index + primitive type flag
    public struct PrimRef
    {
        public Vector3 min, max;
        public int objIndex;
        public int primType; // 0 = sphere, 1 = triangle, 2 = box
        public int primSubIndex;
    }

    public List<BVHNode> nodes = new List<BVHNode>();
    public List<PrimRef> prims = new List<PrimRef>();
    public List<int> primIndices = new List<int>();

    // arrays auxiliares que vamos preencher para GPU (world-space)
    public List<Vector3> primMin = new List<Vector3>();
    public List<Vector3> primMax = new List<Vector3>();
    public List<int> primType = new List<int>();
    public List<int> primObjIndex = new List<int>();

    // para triângulos (world-space vertices)
    public List<Vector3> primTriV0 = new List<Vector3>();
    public List<Vector3> primTriV1 = new List<Vector3>();
    public List<Vector3> primTriV2 = new List<Vector3>();

    // para esferas (centro + raio no espaço world) — aproximação pela bbox com escala isotrópica
    public List<Vector4> primSphereCenterRadius = new List<Vector4>();

    // Simple entry: gather AABBs per object
    public void GatherPrimitives(List<ObjectData> sceneObjects, List<Transformation> transformations)
    {
        prims.Clear();
        primMin.Clear();
        primMax.Clear();
        primType.Clear();
        primObjIndex.Clear();
        primTriV0.Clear(); primTriV1.Clear(); primTriV2.Clear();
        primSphereCenterRadius.Clear();
        primIndices.Clear();

        for (int i = 0; i < sceneObjects.Count; i++)
        {
            var obj = sceneObjects[i];

            if (obj is SphereData s)
            {
                // sphere: assume unit sphere centered at origin with radius 0.5 local
                Transformation T = transformations[s.transformationIndex];
                Matrix4x4 M = T.GetMatrix();
                // compute world-space bbox corners (transform unit sphere bbox)
                Vector3 localMin = new Vector3(-0.5f, -0.5f, -0.5f);
                Vector3 localMax = new Vector3(0.5f, 0.5f, 0.5f);
                Vector3 wmin = M.MultiplyPoint(localMin);
                Vector3 wmax = M.MultiplyPoint(localMax);
                Vector3 mn = Vector3.Min(wmin, wmax);
                Vector3 mx = Vector3.Max(wmin, wmax);

                prims.Add(new PrimRef { min = mn, max = mx, objIndex = i, primType = 0, primSubIndex = 0 });

                primMin.Add(mn); primMax.Add(mx); primType.Add(0); primObjIndex.Add(i);

                // estimate center and radius in world
                Vector3 center = M.MultiplyPoint(Vector3.zero);
                // approximate radius by scaling the local radius by max scale component
                Vector3 scale = new Vector3(T.scale.x, T.scale.y, T.scale.z);
                float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                float radius = 0.5f * maxScale;
                primSphereCenterRadius.Add(new Vector4(center.x, center.y, center.z, radius));

                primTriV0.Add(Vector3.zero); primTriV1.Add(Vector3.zero); primTriV2.Add(Vector3.zero);
            }
            else if (obj is TrianglePrimitive tri)
            {
                Transformation T = transformations[tri.transformationIndex];
                Matrix4x4 M = T.GetMatrix();

                Vector3 a = M.MultiplyPoint(tri.v1);
                Vector3 b = M.MultiplyPoint(tri.v2);
                Vector3 c = M.MultiplyPoint(tri.v3);

                Vector3 mn = Vector3.Min(a, Vector3.Min(b, c));
                Vector3 mx = Vector3.Max(a, Vector3.Max(b, c));

                prims.Add(new PrimRef { min = mn, max = mx, objIndex = i, primType = 1, primSubIndex = 0 });

                primMin.Add(mn); primMax.Add(mx); primType.Add(1); primObjIndex.Add(i);

                primTriV0.Add(a); primTriV1.Add(b); primTriV2.Add(c);
                primSphereCenterRadius.Add(Vector4.zero);
            }
            else if (obj is BoxData box)
            {
                Transformation T = transformations[box.transformationIndex];
                Matrix4x4 M = T.GetMatrix();

                Vector3 min = M.MultiplyPoint(box.min);
                Vector3 max = M.MultiplyPoint(box.max);
                Vector3 mn = Vector3.Min(min, max);
                Vector3 mx = Vector3.Max(min, max);

                prims.Add(new PrimRef { min = mn, max = mx, objIndex = i, primType = 2, primSubIndex = 0 });

                primMin.Add(mn); primMax.Add(mx); primType.Add(2); primObjIndex.Add(i);

                primTriV0.Add(Vector3.zero); primTriV1.Add(Vector3.zero); primTriV2.Add(Vector3.zero);
                primSphereCenterRadius.Add(Vector4.zero);
            }
        }

        // initial indices
        primIndices.Clear();
        for (int i = 0; i < prims.Count; i++) primIndices.Add(i);
    }

    public void BuildRecursive()
    {
        nodes.Clear();
        if (prims.Count == 0) return;
        // indices passed by reference
        BuildNode(0, primIndices.Count, 0);
    }

    private int BuildNode(int start, int count, int depth)
    {
        // create node placeholder
        BVHNode node = new BVHNode();
        // compute bbox of range
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = start; i < start + count; i++)
        {
            var p = prims[primIndices[i]];
            min = Vector3.Min(min, p.min);
            max = Vector3.Max(max, p.max);
        }

        node.boundsMin = min; node.boundsMax = max;
        node.left = -1; node.right = -1; node.firstPrim = -1; node.primCount = 0;

        int nodeIndex = nodes.Count;
        nodes.Add(node); // reserve

        if (count <= 2 || depth > 32)
        {
            // make leaf
            node = nodes[nodeIndex];
            node.firstPrim = start;
            node.primCount = count;
            nodes[nodeIndex] = node;
            return nodeIndex;
        }

        // choose axis by largest extent
        Vector3 extent = max - min;
        int axis = 0;
        if (extent.y > extent.x && extent.y > extent.z) axis = 1;
        else if (extent.z > extent.x && extent.z > extent.y) axis = 2;

        // sort range by centroid along axis (median split)
        primIndices.Sort(start, count, Comparer<int>.Create((iA, iB) =>
        {
            float ca = (prims[iA].min[axis] + prims[iA].max[axis]) * 0.5f;
            float cb = (prims[iB].min[axis] + prims[iB].max[axis]) * 0.5f;
            return ca.CompareTo(cb);
        }));

        int mid = start + count / 2;
        int left = BuildNode(start, mid - start, depth + 1);
        int right = BuildNode(mid, start + count - mid, depth + 1);

        node = nodes[nodeIndex];
        node.left = left;
        node.right = right;
        nodes[nodeIndex] = node;
        return nodeIndex;
    }

    // packs nodes to arrays for GPU upload (float arrays or structured buffers)
    public void PackForGPU(out Vector3[] nodeMins, out Vector3[] nodeMaxs, out int[] nodeLeft, out int[] nodeRight, out int[] nodeFirstPrim, out int[] nodePrimCount,
                           out Vector3[] outPrimMin, out Vector3[] outPrimMax, out int[] outPrimType, out int[] outPrimObjIndex,
                           out Vector3[] outTriV0, out Vector3[] outTriV1, out Vector3[] outTriV2, out Vector4[] outSphereCenterRadius)
    {
        int N = nodes.Count;
        nodeMins = new Vector3[N];
        nodeMaxs = new Vector3[N];
        nodeLeft = new int[N];
        nodeRight = new int[N];
        nodeFirstPrim = new int[N];
        nodePrimCount = new int[N];

        for (int i = 0; i < N; i++)
        {
            nodeMins[i] = nodes[i].boundsMin;
            nodeMaxs[i] = nodes[i].boundsMax;
            nodeLeft[i] = nodes[i].left;
            nodeRight[i] = nodes[i].right;
            nodeFirstPrim[i] = nodes[i].firstPrim;
            nodePrimCount[i] = nodes[i].primCount;
        }

        int P = prims.Count;
        outPrimMin = primMin.ToArray();
        outPrimMax = primMax.ToArray();
        outPrimType = primType.ToArray();
        outPrimObjIndex = primObjIndex.ToArray();
        outTriV0 = primTriV0.ToArray();
        outTriV1 = primTriV1.ToArray();
        outTriV2 = primTriV2.ToArray();
        outSphereCenterRadius = primSphereCenterRadius.ToArray();
    }
}
