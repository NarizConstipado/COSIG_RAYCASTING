using UnityEngine;
using System.Collections.Generic;

public struct Hit
{
    public bool found;
    public float tmin;
    public Vector3 point;
    public Vector3 normal;
    public MaterialProperties material;
}