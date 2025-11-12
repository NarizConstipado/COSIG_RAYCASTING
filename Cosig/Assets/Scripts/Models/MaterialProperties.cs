[System.Serializable]
    public class MaterialProperties(double colorR, double colorG, double colorB, double amb, double dif, double spec, double refr, double refrI)
    {
        public Color color = new Color(colorR, colorG, colorB);
        public float ambient = amb;
        public float diffuse = dif;
        public float specular = spec;
        public float refraction = refr;
        public float refractionIndex = refrI;
    }