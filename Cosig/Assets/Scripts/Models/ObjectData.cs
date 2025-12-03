using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Models
{
    [System.Serializable]
    public class ImageSettings : ObjectData
    {
        public Vector2Int size;
        public Color backgroundColor;

        public ImageSettings(int resX, int resY, float colorR, float colorG, float colorB)
        {
            size = new Vector2Int(resX, resY);
            backgroundColor = new Color(colorR, colorG, colorB);
        }
    }

    [System.Serializable]
    public class CameraData : ObjectData
    {
        public int transformationIndex;
        public float distance;
        public float fov;

        public CameraData(int tIndex, float dist, float fovDegree)
        {
            transformationIndex = tIndex;
            distance = dist;
            fov = fovDegree;
        }
        
    }

    [System.Serializable]
    public class LightData
    {
        public int transformationIndex;
        public Color color;

        public LightData(int tIndex, float r, float g, float b)
        {
            transformationIndex = tIndex;
            color = new Color((float)r, (float)g, (float)b);
        }
    }

    [System.Serializable]
    public class TrianglePrimitive : ObjectData
    {
        public int transformationIndex;
        public int materialIndex;
        public Vector3 v1, v2, v3;

        public TrianglePrimitive(int tIndex, int mIndex, float v1x, float v1y, float v1z,float v2x, float v2y, float v2z,float v3x, float v3y, float v3z)
        {
            transformationIndex = tIndex;
            materialIndex = mIndex;
            v1 = new Vector3(v1x, v1y, v1z);
            v2 = new Vector3(v2x, v2y, v2z);
            v3 = new Vector3(v3x, v3y, v3z);
        }

        public void Intersect(Ray rayWorld, List<Transformation> transformations, List<MaterialProperties> materials, ref Hit hit)
        {
            const float EPS = 1e-6f;
            Transformation T = transformations[transformationIndex];
            MaterialProperties mat = materials[materialIndex];

            // Transformar o raio para o espaço local
            Matrix4x4 Tinv = T.GetInverseMatrix();
            Vector3 ro = Tinv.MultiplyPoint(rayWorld.origin);
            Vector3 rd = Tinv.MultiplyVector(rayWorld.direction).normalized;

            // Möller–Trumbore
            Vector3 e1 = v2 - v1;
            Vector3 e2 = v3 - v1;
            Vector3 pvec = Vector3.Cross(rd, e2);
            float det = Vector3.Dot(e1, pvec);

            if (Mathf.Abs(det) < EPS) return; // Raio paralelo ao triângulo

            float invDet = 1f / det;
            Vector3 tvec = ro - v1;
            float u = Vector3.Dot(tvec, pvec) * invDet;
            if (u < -EPS || u > 1f + EPS) return;

            Vector3 qvec = Vector3.Cross(tvec, e1);
            float v = Vector3.Dot(rd, qvec) * invDet;
            if (v < -EPS || u + v > 1f + EPS) return;

            float tLocal = Vector3.Dot(e2, qvec) * invDet;
            if (tLocal <= EPS) return;

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
    }

    [System.Serializable]
    public class SphereData : ObjectData
    {
        public int transformationIndex;
        public int materialIndex;

        private Vector3 localCenter = Vector3.zero;
        private float localRadius = 0.5f;

        public SphereData(int tIndex, int mIndex)
        {
            transformationIndex = tIndex;
            materialIndex = mIndex;
        }

        public void Intersect(Ray rayWorld, List<Transformation> transformations, List<MaterialProperties> materials, ref Hit hit)
        {
            const float EPS = 1e-6f;

            // Segurança: valida index
            if (transformationIndex < 0 || transformationIndex >= transformations.Count)
            {
                Debug.LogWarning("Sphere has invalid transformationIndex: " + transformationIndex);
                return;
            }

            Transformation T = transformations[transformationIndex];

            // Matriz inversa para trazer o raio para o espaço local do objecto
            Matrix4x4 Tinv = T.GetInverseMatrix();

            // Transformar origem e direcção (direcção sem translação)
            Vector3 localOrigin = Tinv.MultiplyPoint(rayWorld.origin);
            Vector3 localDir = Tinv.MultiplyVector(rayWorld.direction); // NÃO normalizamos ainda

            // IMPORTANTE: normalizar depois da transformação — se o transform contém escala, normalizar aqui é OK
            localDir.Normalize();

            Ray localRay = new Ray(localOrigin, localDir);

            // Parametros da esfera no espaço local (sphere unitária centrada na origem com raio 0.5)
            Vector3 localCenter = Vector3.zero;
            float localRadius = 0.5f;

            // Interseção clássico: a*t^2 + b*t + c = 0
            Vector3 L = localRay.origin - localCenter;
            float a = Vector3.Dot(localRay.direction, localRay.direction);
            float b = 2f * Vector3.Dot(localRay.direction, L);
            float c = Vector3.Dot(L, L) - localRadius * localRadius;

            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f) return;

            float sqrtD = Mathf.Sqrt(discriminant);
            float t0 = (-b - sqrtD) / (2f * a);
            float t1 = (-b + sqrtD) / (2f * a);

            // escolher o primeiro t positivo maior que EPS
            float tLocal = (t0 > EPS) ? t0 : ((t1 > EPS) ? t1 : -1f);
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
            if (tWorld > EPS && tWorld < hit.tmin)
            {
                hit.found = true;
                hit.tmin = tWorld;
                hit.point = worldPoint;
                hit.normal = worldNormal;
                hit.material = materials[materialIndex];
            }
        }
    }

    [System.Serializable]
    public class BoxData : ObjectData
    {
        public int transformationIndex;
        public int materialIndex;
        public Vector3 min = new Vector3(-0.5f, -0.5f, -0.5f);
        public Vector3 max = new Vector3(0.5f, 0.5f, 0.5f);

        public BoxData(int tIndex, int mIndex)
        {
            transformationIndex = tIndex;
            materialIndex = mIndex;
        }

        public void Intersect(Ray ray, List<Transformation> transformations, List<MaterialProperties> materials, ref Hit hit)
        {
            const float EPS = 1e-6f;
            Transformation T = transformations[transformationIndex];

            Matrix4x4 Tinv = T.GetInverseMatrix();
            // Ray no espaço local
            Vector3 localOrigin = T.GetInverseMatrix().MultiplyPoint(ray.origin);
            Vector3 localDir    = T.GetInverseMatrix().MultiplyVector(ray.direction).normalized;
            Ray localRay = new Ray(localOrigin, localDir);

            float tnear = float.NegativeInfinity;
            float tfar = float.PositiveInfinity;

            // ver cada eixo
            for (int i = 0; i < 3; i++)
            {
                float origin = localRay.origin[i];
                float dir = localRay.direction[i];
                float minVal = min[i];
                float maxVal = max[i];

                if (Mathf.Abs(dir) < EPS)
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
                if (Mathf.Abs(localP[i] - max[i]) < EPS) { worldNormal[i] = 1f; break; }
                if (Mathf.Abs(localP[i] - min[i]) < EPS) { worldNormal[i] = -1f; break; }
            }

            worldNormal = T.GetInverseTransposeMatrix().MultiplyVector(worldNormal).normalized;

            float tWorld = (worldPoint - ray.origin).magnitude;
            if (tWorld > EPS && tWorld < hit.tmin)
            {
                hit.found = true;
                hit.tmin = tWorld;
                hit.point = worldPoint;
                hit.normal = worldNormal;
                hit.material = materials[materialIndex];
            }
        }
    }

    [System.Serializable]
    public abstract class ObjectData
    {
    }

    [System.Serializable]
    public class SerializableObject
    {
        public string type;
        public TrianglePrimitive triangle;
        public SphereData sphere;
        public BoxData box;
        public CameraData camera;
        public ImageSettings image;
    }
}
