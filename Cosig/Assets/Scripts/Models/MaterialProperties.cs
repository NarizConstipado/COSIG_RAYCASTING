using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Serializable]
public class MaterialProperties
{
    public Color color;
    public double ambient;
    public double diffuse;
    public double specular;
    public double refraction;
    public double refractionIndex;

    public MaterialProperties(float colorR, float colorG, float colorB, double amb, double dif, double spec, double refr, double refrI)
    {
        color = new Color(colorR, colorG, colorB);
        ambient = amb;
        diffuse = dif;
        specular = spec;
        refraction = refr;
        refractionIndex = refrI;
    }
}