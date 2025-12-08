using Models;
using UnityEngine;
using System.IO;

public class GPUPrimaryRays : MonoBehaviour
{
    public ComputeShader rayShader;
    public RenderTexture outputTexture;
    private Camera cam;
    public GPUObject[] gpuObjects;
    public GPUMaterial[] gpuMaterials;
    public GPULight[] gpuLights;

    // Resolução configurada dinamicamente
    private int width = 512;
    private int height = 512;

    // BVH arrays (preenchidos por SceneBuilder antes de chamar Render)
    public Vector3[] bvhNodeMins;
    public Vector3[] bvhNodeMaxs;
    public int[] bvhNodeLeft;
    public int[] bvhNodeRight;
    public int[] bvhNodeFirstPrim;
    public int[] bvhNodePrimCount;

    // prim arrays
    public Vector3[] primMin;
    public Vector3[] primMax;
    public int[] primType;
    public int[] primObjIndex;

    public Vector3[] primTriV0;
    public Vector3[] primTriV1;
    public Vector3[] primTriV2;
    public Vector4[] primSphereCenterRadius;

    public void Render(ImageSettings imgSettings)
    {
        if (imgSettings != null)
        {
            // Atualiza os campos da instância com a resolução correta (multiplo de 8 para dispatch)
            this.width = Mathf.CeilToInt(imgSettings.size.x / 8f) * 8;
            this.height = Mathf.CeilToInt(imgSettings.size.y / 8f) * 8;
        }

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No MainCamera found in scene. Please add a camera with the MainCamera tag.");
                return;
            }
        }

        outputTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        int kernel = rayShader.FindKernel("CSMain");

        // Objetos / materiais / luzes
        ComputeBuffer objBuffer = new ComputeBuffer(gpuObjects.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPUObject)));
        objBuffer.SetData(gpuObjects);
        rayShader.SetBuffer(kernel, "objects", objBuffer);

        ComputeBuffer matBuffer = new ComputeBuffer(gpuMaterials.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPUMaterial)));
        matBuffer.SetData(gpuMaterials);
        rayShader.SetBuffer(kernel, "materials", matBuffer);

        ComputeBuffer lightBuffer = new ComputeBuffer(gpuLights.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPULight)));
        lightBuffer.SetData(gpuLights);
        rayShader.SetBuffer(kernel, "lights", lightBuffer);

        // BVH buffers (opcionais)
        ComputeBuffer nodeMinBuf = null, nodeMaxBuf = null, nodeLeftBuf = null, nodeRightBuf = null, nodeFirstPrimBuf = null, nodePrimCountBuf = null;
        ComputeBuffer primObjIndexBuf = null;

        if (bvhNodeMins != null && bvhNodeMins.Length > 0)
        {
            nodeMinBuf = new ComputeBuffer(bvhNodeMins.Length, sizeof(float) * 3);
            nodeMinBuf.SetData(bvhNodeMins);
            rayShader.SetBuffer(kernel, "bvhNodeMins", nodeMinBuf);

            nodeMaxBuf = new ComputeBuffer(bvhNodeMaxs.Length, sizeof(float) * 3);
            nodeMaxBuf.SetData(bvhNodeMaxs);
            rayShader.SetBuffer(kernel, "bvhNodeMaxs", nodeMaxBuf);

            nodeLeftBuf = new ComputeBuffer(bvhNodeLeft.Length, sizeof(int));
            nodeLeftBuf.SetData(bvhNodeLeft);
            rayShader.SetBuffer(kernel, "bvhNodeLeft", nodeLeftBuf);

            nodeRightBuf = new ComputeBuffer(bvhNodeRight.Length, sizeof(int));
            nodeRightBuf.SetData(bvhNodeRight);
            rayShader.SetBuffer(kernel, "bvhNodeRight", nodeRightBuf);

            nodeFirstPrimBuf = new ComputeBuffer(bvhNodeFirstPrim.Length, sizeof(int));
            nodeFirstPrimBuf.SetData(bvhNodeFirstPrim);
            rayShader.SetBuffer(kernel, "bvhNodeFirstPrim", nodeFirstPrimBuf);

            nodePrimCountBuf = new ComputeBuffer(bvhNodePrimCount.Length, sizeof(int));
            nodePrimCountBuf.SetData(bvhNodePrimCount);
            rayShader.SetBuffer(kernel, "bvhNodePrimCount", nodePrimCountBuf);

            // prim -> obj index
            if (primObjIndex != null && primObjIndex.Length > 0)
            {
                primObjIndexBuf = new ComputeBuffer(primObjIndex.Length, sizeof(int));
                primObjIndexBuf.SetData(primObjIndex);
                rayShader.SetBuffer(kernel, "primObjIndex", primObjIndexBuf);
            }

            // passar contagens ao shader
            rayShader.SetInt("bvhNodeCount", bvhNodeMins.Length);
            rayShader.SetInt("primCount", primObjIndex != null ? primObjIndex.Length : 0);
        }
        else
        {
            rayShader.SetInt("bvhNodeCount", 0);
            rayShader.SetInt("primCount", 0);
        }

        rayShader.SetTexture(kernel, "Result", outputTexture);

        rayShader.SetVector("camPos", cam.transform.position);
        rayShader.SetVector("camForward", cam.transform.forward);
        rayShader.SetVector("camRight", cam.transform.right);
        rayShader.SetVector("camUp", cam.transform.up);
        rayShader.SetFloat("fov", (float)cam.fieldOfView * Mathf.Deg2Rad);
        rayShader.SetInt("width", width);
        rayShader.SetInt("height", height);

        int threadX = Mathf.CeilToInt(width / 8f);
        int threadY = Mathf.CeilToInt(height / 8f);
        rayShader.Dispatch(kernel, threadX, threadY, 1);

        // Limpeza buffers
        objBuffer.Dispose();
        matBuffer.Dispose();
        lightBuffer.Dispose();

        nodeMinBuf?.Dispose();
        nodeMaxBuf?.Dispose();
        nodeLeftBuf?.Dispose();
        nodeRightBuf?.Dispose();
        nodeFirstPrimBuf?.Dispose();
        nodePrimCountBuf?.Dispose();
        primObjIndexBuf?.Dispose();

        SaveRenderTextureToPNG(outputTexture, Application.dataPath + "/RayTraceOutput_gpu.png");

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.mainTexture = outputTexture;
    }

    private void SaveRenderTextureToPNG(RenderTexture rt, string path)
    {
        RenderTexture activeRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        Debug.Log("Saved GPU raytraced image to: " + path);

        RenderTexture.active = activeRT;
        Destroy(tex);
    }
}