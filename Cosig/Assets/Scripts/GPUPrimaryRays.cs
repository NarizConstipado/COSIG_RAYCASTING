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

    // Deve ser chamada pelo SceneBuilder, passando ImageSettings
    public void Render(ImageSettings imgSettings)
    {
        if (imgSettings != null)
        {
            int width = Mathf.CeilToInt(imgSettings.size.x / 8f) * 8;
            int height = Mathf.CeilToInt(imgSettings.size.y / 8f) * 8;
        }

        // Procura a câmera principal automaticamente
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No MainCamera found in scene. Please add a camera with the MainCamera tag.");
                return;
            }
        }

        // Cria RenderTexture para a GPU
        outputTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        int kernel = rayShader.FindKernel("CSMain");

        // Buffers GPU
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

        // Camera params
        rayShader.SetVector("camPos", cam.transform.position);
        rayShader.SetVector("camForward", cam.transform.forward);
        rayShader.SetVector("camRight", cam.transform.right);
        rayShader.SetVector("camUp", cam.transform.up);
        rayShader.SetFloat("fov", (float)cam.fieldOfView * Mathf.Deg2Rad);
        rayShader.SetInt("width", width);
        rayShader.SetInt("height", height);

        // Dispatch
        int threadX = Mathf.CeilToInt(width / 8f);
        int threadY = Mathf.CeilToInt(height / 8f);
        rayShader.Dispatch(kernel, threadX, threadY, 1);

        // Limpeza buffers
        objBuffer.Dispose();
        matBuffer.Dispose();
        lightBuffer.Dispose();

        // Copia RenderTexture para PNG
        SaveRenderTextureToPNG(outputTexture, Application.dataPath + "/RayTraceOutput.png");

        // Opcional: atribui a RawImage ou quad para visualização
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
