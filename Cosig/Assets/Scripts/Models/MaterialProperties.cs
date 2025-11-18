using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Serializable]
public class MaterialProperties
{
    public Color color;
    public float ambient;
    public float diffuse;
    public float specular;
    public float refraction;
    public float refractionIndex;

    public MaterialProperties(float colorR, float colorG, float colorB, float amb, float dif, float spec, float refr, float refrI)
    {
        color = new Color(colorR, colorG, colorB);
        ambient = amb;
        diffuse = dif;
        specular = spec;
        refraction = refr;
        refractionIndex = refrI;
    }
}