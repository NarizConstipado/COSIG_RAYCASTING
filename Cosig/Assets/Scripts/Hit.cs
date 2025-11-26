using UnityEngine;
using System.Collections.Generic;

public struct Hit
{
    public bool found;       // True se houve interseção
    public float tmin;       // Distância mínima do raio à interseção
    public Vector3 point;    // Ponto de interseção no espaço mundo
    public Vector3 normal;   // Normal no ponto de interseção
    public MaterialProperties material; // Material do objeto intersectado
}