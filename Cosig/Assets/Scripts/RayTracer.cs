using Models;
using System.Collections.Generic;
using UnityEditor.TerrainTools;
using UnityEngine;

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

        Vector3 origin = transformations[camera.transformationIndex].translation;
        origin.z += camera.distance;

        float fovRad = camera.fov * Mathf.PI / 180f;

        float height = 2f * camera.distance * Mathf.Tan(fovRad / 2f);
        float width = height * Hres / (float)Vres;

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
            if (obj is SphereData sphere) IntersectSphere(sphere, ray, ref hit);
            else if (obj is BoxData box) IntersectBox(box, ray, ref hit);
            else if (obj is TrianglePrimitive tri) IntersectTriangle(tri, ray, ref hit);
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
                    if (obj is SphereData sphere) IntersectSphere(sphere, shadowRay, ref shadowHit);
                    else if (obj is BoxData box) IntersectBox(box, shadowRay, ref shadowHit);
                    else if (obj is TrianglePrimitive tri) IntersectTriangle(tri, shadowRay, ref shadowHit);

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

    private void IntersectTriangle(TrianglePrimitive tri, Ray rayWorld, ref Hit hit)
    {
        const float epsilon = 1e-6f;
        Transformation T = transformations[tri.transformationIndex];
        MaterialProperties mat = materials[tri.materialIndex];

        //Raio no espaço local
        Matrix4x4 Tinv = T.GetInverseMatrix();
        Vector3 ro = Tinv.MultiplyPoint(rayWorld.origin);
        Vector3 rd = Tinv.MultiplyVector(rayWorld.direction).normalized;

        // Möller–Trumbore
        Vector3 e1 = tri.v2 - tri.v1;
        Vector3 e2 = tri.v3 - tri.v1;
        Vector3 pvec = Vector3.Cross(rd, e2);
        float det = Vector3.Dot(e1, pvec);

        if (Mathf.Abs(det) < epsilon) return;

        float invDet = 1f / det;
        Vector3 tvec = ro - tri.v1;
        float u = Vector3.Dot(tvec, pvec) * invDet;
        if (u < -epsilon || u > 1f + epsilon) return;

        Vector3 qvec = Vector3.Cross(tvec, e1);
        float v = Vector3.Dot(rd, qvec) * invDet;
        if (v < -epsilon || u + v > 1f + epsilon) return;

        float tLocal = Vector3.Dot(e2, qvec) * invDet;
        if (tLocal <= epsilon) return;

        // Ponto e normal no espaço local
        Vector3 pLocal = ro + tLocal * rd;
        Vector3 nLocal = Vector3.Cross(e1, e2).normalized;

        // Converter para o espaço do mundo
        Vector3 pWorld = T.GetMatrix().MultiplyPoint(pLocal);
        Vector3 nWorld = T.GetInverseTransposeMatrix().MultiplyVector(nLocal).normalized;
        float tWorld = (pWorld - rayWorld.origin).magnitude;

        if (tWorld < hit.tmin)
        {
            hit.found = true;
            hit.tmin = tWorld;
            hit.point = pWorld;
            hit.normal = nWorld;
            hit.material = mat;
        }
    }
    public void IntersectSphere(SphereData sphere, Ray rayWorld, ref Hit hit)
    {
        const float epsilon = 1e-6f;

        if (sphere.transformationIndex < 0 || sphere.transformationIndex >= transformations.Count)
        {
            Debug.LogWarning("Sphere has invalid transformationIndex: " + sphere.transformationIndex);
            return;
        }

        Transformation T = transformations[sphere.transformationIndex];

        // Raio no espaço local
        Matrix4x4 Tinv = T.GetInverseMatrix();
        Vector3 localOrigin = Tinv.MultiplyPoint(rayWorld.origin);
        Vector3 localDir = Tinv.MultiplyVector(rayWorld.direction);

        localDir.Normalize();

        Ray localRay = new Ray(localOrigin, localDir);

        // Parametros da esfera no espaço local
        Vector3 localCenter = Vector3.zero;
        float localRadius = 0.5f;

        //a*t^2 + b*t + c = 0
        Vector3 L = localRay.origin - localCenter;
        float a = Vector3.Dot(localRay.direction, localRay.direction);
        float b = 2f * Vector3.Dot(localRay.direction, L);
        float c = Vector3.Dot(L, L) - localRadius * localRadius;

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return;

        float sqrtD = Mathf.Sqrt(discriminant);
        float t0 = (-b - sqrtD) / (2f * a);
        float t1 = (-b + sqrtD) / (2f * a);

        // escolher o primeiro t positivo maior que epsilon
        float tLocal = (t0 > epsilon) ? t0 : ((t1 > epsilon) ? t1 : -1f);
        if (tLocal < 0f) return;

        // Ponto e normal no espaço local
        Vector3 localPoint = localRay.origin + tLocal * localRay.direction;
        Vector3 localNormal = (localPoint - localCenter).normalized;

        // Converter para espaço world
        Vector3 worldPoint = T.GetMatrix().MultiplyPoint(localPoint);
        Vector3 worldNormal = T.GetInverseTransposeMatrix().MultiplyVector(localNormal).normalized;

        // Distância no espaço world (para comparação com hit.tmin)
        float tWorld = (worldPoint - rayWorld.origin).magnitude;

        // Atualiza hit se for mais perto
        if (tWorld > epsilon && tWorld < hit.tmin)
        {
            hit.found = true;
            hit.tmin = tWorld;
            hit.point = worldPoint;
            hit.normal = worldNormal;
            hit.material = materials[sphere.materialIndex];
        }
    }
    public void IntersectBox(BoxData box, Ray ray, ref Hit hit)
    {
        const float epsilon = 1e-6f;
        Transformation T = transformations[box.transformationIndex];

        //Raio no espaço local
        Matrix4x4 Tinv = T.GetInverseMatrix();
        Vector3 localOrigin = Tinv.MultiplyPoint(ray.origin);
        Vector3 localDir = Tinv.MultiplyVector(ray.direction).normalized;
        Ray localRay = new Ray(localOrigin, localDir);

        float tnear = float.NegativeInfinity;
        float tfar = float.PositiveInfinity;

        // ver cada eixo
        for (int i = 0; i < 3; i++)
        {
            float origin = localRay.origin[i];
            float dir = localRay.direction[i];
            float minVal = box.min[i];
            float maxVal = box.max[i];

            if (Mathf.Abs(dir) < epsilon)
            {
                // Raio paralelo aos planos
                if (origin < minVal || origin > maxVal)
                    return;
            }
            else
            {
                // Calcular interseção com os planos
                float t1 = (minVal - origin) / dir;
                float t2 = (maxVal - origin) / dir;
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                tnear = Mathf.Max(tnear, t1);
                tfar = Mathf.Min(tfar, t2);
                if (tnear > tfar || tfar < 0f)
                    return;
            }
        }

        Vector3 localPoint = localRay.origin + tnear * localRay.direction;
        Vector3 worldPoint = T.GetMatrix().MultiplyPoint(localPoint);

        // encontra o eixo mais próximo da face
        Vector3 worldNormal = Vector3.zero;
        Vector3 localP = localPoint;

        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(localP[i] - box.max[i]) < epsilon) { worldNormal[i] = 1f; break; }
            if (Mathf.Abs(localP[i] - box.min[i]) < epsilon) { worldNormal[i] = -1f; break; }
        }

        worldNormal = T.GetInverseTransposeMatrix().MultiplyVector(worldNormal).normalized;

        float tWorld = (worldPoint - ray.origin).magnitude;
        if (tWorld > epsilon && tWorld < hit.tmin)
        {
            hit.found = true;
            hit.tmin = tWorld;
            hit.point = worldPoint;
            hit.normal = worldNormal;
            hit.material = materials[box.materialIndex];
        }
    }
}