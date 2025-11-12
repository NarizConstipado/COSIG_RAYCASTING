// Services/SceneService.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Models;
using System.Globalization;

namespace Services
{
    // Service responsible for loading and interpreting data from a scene configuration file
    public class SceneService
    {
        // Method to load scene objects from a given configuration file path
        public void LoadScene(string filePath, out List<ObjectData> sceneObjects, out List<Transformation> transformations, out List<MaterialProperties> materials)
        {
            sceneObjects = new List<ObjectData>();
            transformations = new List<Transformation>();
            materials = new List<MaterialProperties>();
            // Check if the file exists before proceeding
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found at {filePath}");
                return;
            }
            // Read all lines from the configuration file
            string[] lines = File.ReadAllLines(filePath);
            int currentLine = 0;

            while (currentLine < lines.Length)
            {
                string line = lines[currentLine].Trim();

                if (line.StartsWith("Transformation"))
                {
                    currentLine++;
                    Transformation t = new Transformation(0,0,0,0,0,0,0,0,0);
                    while (currentLine < lines.Length && !lines[currentLine].Contains('}'))
                    {
                        string[] parts = lines[currentLine].Trim().Split(' ');
                        if (parts[0] == "T")
                            t.translation = new Vector3(float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture));
                        else if (parts[0] == "Rx")
                            t.rotation.x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        else if (parts[0] == "Ry")
                            t.rotation.y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        else if (parts[0] == "Rz")
                            t.rotation.z = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        else if (parts[0] == "S")
                            t.scale = new Vector3(float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture));
                        currentLine++;
                    }
                    transformations.Add(t);
                }

                else if (line.StartsWith("Material"))
                {
                    currentLine++;
                    
                    string[] colorLine = lines[currentLine].Trim().Split(' ');
                    float r = float.Parse(colorLine[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(colorLine[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(colorLine[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    string[] coefLine = lines[currentLine].Trim().Split(' ');
                    float amb = float.Parse(coefLine[0], CultureInfo.InvariantCulture);
                    float dif = float.Parse(coefLine[1], CultureInfo.InvariantCulture);
                    float spec = float.Parse(coefLine[2], CultureInfo.InvariantCulture);
                    float refr = float.Parse(coefLine[3], CultureInfo.InvariantCulture);
                    float refrI = float.Parse(coefLine[4], CultureInfo.InvariantCulture);

                    materials.Add(new MaterialProperties(r, g, b, amb, dif, spec, refr, refrI));
                }

                else if (line.StartsWith("Sphere") || line.StartsWith("Box"))
                {
                    currentLine++;
                    string[] index = lines[currentLine].Trim().Split(' ');
                    int tIndex = int.Parse(index[0]);

                    currentLine++;
                    index = lines[currentLine].Trim().Split(' ');
                    int mIndex = int.Parse(index[0]);

                    if (line.StartsWith("Sphere")) sceneObjects.Add(new SphereData(tIndex, mIndex));
                    if (line.StartsWith("Box")) sceneObjects.Add(new BoxData(tIndex, mIndex));
                }

                else if (line.StartsWith("Triangles"))
                {
                    currentLine++;
                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;
                    while (currentLine < lines.Length && !lines[currentLine].Contains('}'))
                    {
                        string[] parts = lines[currentLine].Trim().Split(' ');
                        int mIndex = int.Parse(parts[0]);
                        currentLine++;
                        parts = lines[currentLine].Trim().Split(' ');
                        Vector3 v1 = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[2], CultureInfo.InvariantCulture));
                        currentLine++;
                        parts = lines[currentLine].Trim().Split(' ');
                        Vector3 v2 = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[2], CultureInfo.InvariantCulture));
                        currentLine++;
                        parts = lines[currentLine].Trim().Split(' ');
                        Vector3 v3 = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[2], CultureInfo.InvariantCulture));
                        currentLine++;

                        sceneObjects.Add(new TrianglePrimitive(tIndex, mIndex, v1.x, v1.y, v1.z, v2.x, v2.y, v2.z, v3.x, v3.y, v3.z));
                    }
                }

                else if (line.StartsWith("Camera"))
                {
                    currentLine++;
                    string[] parts = lines[currentLine].Trim().Split(' ');
                    int tIndex = int.Parse(parts[0]);
                    currentLine++;
                    float distance = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    currentLine++;
                    float fov = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    sceneObjects.Add(new CameraData(tIndex, distance, fov));
                }

                else if (line.StartsWith("Light"))
                {
                    currentLine++;
                    string[] parts = lines[currentLine].Trim().Split(' ');
                    int tIndex = int.Parse(parts[0]);
                    currentLine++;
                    parts = lines[currentLine].Trim().Split(' ');
                    float r = float.Parse(parts[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    sceneObjects.Add(new LightData(tIndex, r, g, b));
                }
            }
        }
    }
}


/*string[] lines = File.ReadAllLines(filePath);
            int currentLine = 0;

            while (currentLine < lines.Length)
            {
                string line = lines[currentLine].Trim();

                if (line.StartsWith("Transformation"))
                {
                    // Lê bloco entre { }
                    currentLine++;
                    Transformation t = new Transformation();
                    while (currentLine < lines.Length && !lines[currentLine].Contains("}"))
                    {
                        string[] parts = lines[currentLine].Trim().Split(' ');
                        if (parts[0] == "T")
                            t.translation = new Vector3(float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                                                        float.Parse(parts[3], CultureInfo.InvariantCulture));
                        else if (parts[0] == "Rx")
                            t.rotation.x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        else if (parts[0] == "Ry")
                            t.rotation.y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        else if (parts[0] == "Rz")
                            t.rotation.z = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        else if (parts[0] == "S")
                            t.scale = new Vector3(float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                  float.Parse(parts[2], CultureInfo.InvariantCulture),
                                                  float.Parse(parts[3], CultureInfo.InvariantCulture));
                        currentLine++;
                    }
                    transformations.Add(t);
                }
                else if (line.StartsWith("Material"))
                {
                    currentLine++;
                    // Cor
                    string[] colorLine = lines[currentLine].Trim().Split(' ');
                    float r = float.Parse(colorLine[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(colorLine[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(colorLine[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    // Coeficientes: ambient, diffuse, specular, refract, refrIndex
                    string[] coefLine = lines[currentLine].Trim().Split(' ');
                    float amb = float.Parse(coefLine[0], CultureInfo.InvariantCulture);
                    float dif = float.Parse(coefLine[1], CultureInfo.InvariantCulture);
                    float spec = float.Parse(coefLine[2], CultureInfo.InvariantCulture);
                    float refr = float.Parse(coefLine[3], CultureInfo.InvariantCulture);
                    float refrI = float.Parse(coefLine[4], CultureInfo.InvariantCulture);

                    materials.Add(new MaterialProperties(r, g, b, amb, dif, spec, refr, refrI));
                }
                else if (line.StartsWith("Sphere"))
                {
                    currentLine++;
                    string[] indices = lines[currentLine].Trim().Split(' ');
                    int tIndex = int.Parse(indices[0]);
                    int mIndex = int.Parse(indices[1]);
                    sceneObjects.Add(new SphereData(tIndex, mIndex));
                }
                else if (line.StartsWith("Box"))
                {
                    currentLine++;
                    string[] indices = lines[currentLine].Trim().Split(' ');
                    int tIndex = int.Parse(indices[0]);
                    int mIndex = int.Parse(indices[1]);
                    sceneObjects.Add(new BoxData(tIndex, mIndex));
                }
                else if (line.StartsWith("Triangles"))
                {
                    currentLine++;
                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;
                    while (currentLine < lines.Length && !lines[currentLine].Contains("}"))
                    {
                        string[] parts = lines[currentLine].Trim().Split(' ');
                        int mIndex = int.Parse(parts[0]);
                        Vector3 v1 = new Vector3(float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[2], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[3], CultureInfo.InvariantCulture));
                        currentLine++;
                        parts = lines[currentLine].Trim().Split(' ');
                        Vector3 v2 = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[2], CultureInfo.InvariantCulture));
                        currentLine++;
                        parts = lines[currentLine].Trim().Split(' ');
                        Vector3 v3 = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[1], CultureInfo.InvariantCulture),
                                                 float.Parse(parts[2], CultureInfo.InvariantCulture));
                        currentLine++;

                        sceneObjects.Add(new TrianglePrimitive(tIndex, mIndex, v1.x, v1.y, v1.z, v2.x, v2.y, v2.z, v3.x, v3.y, v3.z));
                    }
                }
                else if (line.StartsWith("Camera"))
                {
                    currentLine++;
                    string[] parts = lines[currentLine].Trim().Split(' ');
                    int tIndex = int.Parse(parts[0]);
                    currentLine++;
                    float distance = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    currentLine++;
                    float fov = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    sceneObjects.Add(new CameraData(tIndex, distance, fov));
                }
                else if (line.StartsWith("Light"))
                {
                    currentLine++;
                    string[] parts = lines[currentLine].Trim().Split(' ');
                    int tIndex = int.Parse(parts[0]);
                    float r = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float g = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float b = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    sceneObjects.Add(new LightData(tIndex, r, g, b));
                }

                currentLine++;
            }
        }*/