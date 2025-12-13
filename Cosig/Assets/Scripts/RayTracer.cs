using UnityEngine;
using System.Collections.Generic;
using Models;

public class PrimaryRays
{

    private List<ObjectData> objects;
    private List<LightData> lights;
    private List<Transformation> transformations;
    private List<MaterialProperties> materials;
    private ImageSettings imageSettings;
    private CameraData camera;

    private const float epsilon = 1e-4f;
    

    public PrimaryRays(List<ObjectData> objs, List<LightData> lgts, List<Transformation> trans, List<MaterialProperties> mats, ImageSettings img, CameraData cam)
    {
        objects = objs;
        lights = lgts;
        transformations = trans;
        materials = mats;
        imageSettings = img;
        camera = cam;
    }

    public Texture2D Render()
    {
        int rec = 2; 

        int Hres = imageSettings.size.x;
        int Vres = imageSettings.size.y;

        Texture2D tex = new Texture2D(Hres, Vres);
        tex.filterMode = FilterMode.Point;

        Vector3 origin = new Vector3(0f, 0f, camera.distance);

        // Converter FOV
        float fovRad = camera.fov * Mathf.PI / 180f;

        // Calcular o tamanho do plano de projeção
        float height = 2f * camera.distance * Mathf.Tan(fovRad / 2f);
        float width = height * Hres / (float)Vres;

        // Tamanho do pixel
        float s = height / Vres;

        for (int j = 0; j < Vres; j++)
        {
            for (int i = 0; i < Hres; i++)
            {
                // Centro do pixel no plano
                float Px = (i + 0.5f) * s - width / 2f;
                float Py = -(j + 0.5f) * s + height / 2f;
                float Pz = 0f;

                Vector3 P = new Vector3(Px, Py, Pz);

                Vector3 direction = (P - origin).normalized;

                Ray ray = new Ray(origin, direction);

                Color color = traceRay(ray, rec);

                // Clamp
                color.r = Mathf.Clamp01(color.r);
                color.g = Mathf.Clamp01(color.g);
                color.b = Mathf.Clamp01(color.b);

                // Converter para bytes
                Color32 pixelColor = new Color32(
                    (byte)(color.r * 255f),
                    (byte)(color.g * 255f),
                    (byte)(color.b * 255f),
                    255
                );

                tex.SetPixel(Hres - 1 - i, Vres - 1 - j, pixelColor);
            }
        }

        tex.Apply();
        return tex;
    }

    private Color traceRay(Ray ray, int rec)
    {
        Color finalColor = Color.black;
        Hit hit = new Hit
        {
            found = false,
            tmin = float.PositiveInfinity
        };

        foreach (var obj in objects)
        {
            if (obj is SphereData sphere) sphere.Intersect(ray, transformations, materials, ref hit);
            else if (obj is BoxData box) box.Intersect(ray, transformations, materials, ref hit);
            else if (obj is TrianglePrimitive tri) tri.Intersect(ray, transformations, materials, ref hit);
        }

        if (!hit.found) return finalColor;

        foreach (var light in lights)
        {
            Matrix4x4 LT = transformations[light.transformationIndex].GetMatrix();
            Vector3 lightPos = LT.MultiplyPoint(Vector3.zero);

            finalColor += light.color * hit.material.color * hit.material.ambient;

            Vector3 L = lightPos - hit.point;
            float tLight = L.magnitude;
            L.Normalize();

            float cosTheta = Vector3.Dot(hit.normal, L);

            if (cosTheta > 0f)
            {
                Vector3 shadowOrigin = hit.point + hit.normal * epsilon;

                Ray shadowRay = new Ray(shadowOrigin, L);

                Hit shadowHit = new Hit
                {
                    found = false,
                    tmin = tLight
                };

                foreach (var obj in objects)
                {
                    if (obj is SphereData sphere) sphere.Intersect(shadowRay, transformations, materials, ref shadowHit);
                    else if (obj is BoxData box) box.Intersect(shadowRay, transformations, materials, ref shadowHit);
                    else if (obj is TrianglePrimitive tri) tri.Intersect(shadowRay, transformations, materials, ref shadowHit);

                    if (shadowHit.found)
                        break;
                }

                if (!shadowHit.found)
                {
                    finalColor += light.color * hit.material.color * hit.material.diffuse * cosTheta;
                }
            }
        }

        if (rec > 0)
        {
            float cosThetaV = -Vector3.Dot(ray.direction, hit.normal);

            if (hit.material.specular > 0f)
            {
                if (cosThetaV > 0f)
                {
                    Vector3 r = ray.direction + 2f * cosThetaV * hit.normal;
                    r.Normalize();

                    Ray reflectedRay = new Ray(hit.point + epsilon * r, r);

                    finalColor += hit.material.color * hit.material.specular * traceRay(reflectedRay, rec - 1);
                }
            }

            if(hit.material.refraction > 0f)
            {
                Vector3 N = hit.normal;
                float eta;

                if (cosThetaV > 0f)
                {
                    eta = 1.0f / hit.material.refractionIndex;
                }
                else
                {
                    eta = hit.material.refractionIndex;
                    N = -N;
                    cosThetaV = -cosThetaV;
                }

                float k = 1.0f - eta * eta * (1.0f - cosThetaV * cosThetaV);
                if (k >= 0f)
                {
                    float cosThetaR = Mathf.Sqrt(k);

                    Vector3 refractDir = eta * ray.direction + (eta * cosThetaV - cosThetaR) * N;
                    refractDir.Normalize();

                    Ray refractedRay = new Ray(hit.point - N * epsilon, refractDir);

                    finalColor += hit.material.color * hit.material.refraction * traceRay(refractedRay, rec - 1);
                }
            }
        }

        return finalColor /= lights.Count;
    }
}