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

        ComputeBuffer objBuffer = new ComputeBuffer(gpuObjects.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPUObject)));
        objBuffer.SetData(gpuObjects);
        rayShader.SetBuffer(kernel, "objects", objBuffer);

        ComputeBuffer matBuffer = new ComputeBuffer(gpuMaterials.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPUMaterial)));
        matBuffer.SetData(gpuMaterials);
        rayShader.SetBuffer(kernel, "materials", matBuffer);

        ComputeBuffer lightBuffer = new ComputeBuffer(gpuLights.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GPULight)));
        lightBuffer.SetData(gpuLights);
        rayShader.SetBuffer(kernel, "lights", lightBuffer);

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

        objBuffer.Dispose();
        matBuffer.Dispose();
        lightBuffer.Dispose();

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