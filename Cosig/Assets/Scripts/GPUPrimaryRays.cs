using Models;
using UnityEngine;
using System.IO;

public class GPUPrimaryRays : MonoBehaviour
{
    [SerializeField] private ComputeShader rayShader;
    public RenderTexture outputTexture;
    private Camera cam;
    public GPUObject[] gpuObjects;
    public GPUMaterial[] gpuMaterials;
    public GPULight[] gpuLights;

    private int width = 512;
    private int height = 512;

    public Vector3[] bvhNodeMins;
    public Vector3[] bvhNodeMaxs;
    public int[] bvhNodeLeft;
    public int[] bvhNodeRight;
    public int[] bvhNodeFirstPrim;
    public int[] bvhNodePrimCount;
    public Vector3[] primMin;
    public Vector3[] primMax;
    public int[] primType;
    public int[] primObjIndex;
    
    public Vector3[] primTriV0;
    public Vector3[] primTriV1;
    public Vector3[] primTriV2;
    public Vector4[] primSphereCenterRadius;

    public void Render(ImageSettings imgSettings, int rec)
    {
        if (imgSettings != null)
        {
            this.width = Mathf.CeilToInt(imgSettings.size.x / 8f) * 8;
            this.height = Mathf.CeilToInt(imgSettings.size.y / 8f) * 8;
        }

        cam = GameObject.Find("Camera").GetComponent<Camera>();

        outputTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        int kernel = rayShader.FindKernel("CSMain");

        ComputeBuffer objBuffer = new ComputeBuffer(gpuObjects.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPUObject)));
        objBuffer.SetData(gpuObjects);
        rayShader.SetBuffer(kernel, "objects", objBuffer);

        ComputeBuffer matBuffer = new ComputeBuffer(gpuMaterials.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPUMaterial)));
        matBuffer.SetData(gpuMaterials);
        rayShader.SetBuffer(kernel, "materials", matBuffer);

        ComputeBuffer lightBuffer = new ComputeBuffer(gpuLights.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPULight)));
        lightBuffer.SetData(gpuLights);
        rayShader.SetBuffer(kernel, "lights", lightBuffer);

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

            if (primObjIndex != null && primObjIndex.Length > 0)
            {
                primObjIndexBuf = new ComputeBuffer(primObjIndex.Length, sizeof(int));
                primObjIndexBuf.SetData(primObjIndex);
                rayShader.SetBuffer(kernel, "primObjIndex", primObjIndexBuf);
            }

            rayShader.SetInt("bvhNodeCount", bvhNodeMins.Length);
            rayShader.SetInt("primCount", primObjIndex != null ? primObjIndex.Length : 0);
        }
        else
        {
            rayShader.SetInt("bvhNodeCount", 0);
            rayShader.SetInt("primCount", 0);
        }

        rayShader.SetTexture(kernel, "Result", outputTexture);

        rayShader.SetInt("recursionDepth", rec);
        rayShader.SetVector("bgColor", new Vector3(imgSettings.backgroundColor.r, imgSettings.backgroundColor.g, imgSettings.backgroundColor.b));

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

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.mainTexture = outputTexture;
    }

    public void SaveRenderTextureToPNG(string path)
    {
        RenderTexture activeRT = RenderTexture.active;
        RenderTexture.active = outputTexture;

        Texture2D tex = new Texture2D(outputTexture.width, outputTexture.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        Debug.Log("Saved GPU raytraced image to: " + path);

        RenderTexture.active = activeRT;
        Destroy(tex);
    }
}