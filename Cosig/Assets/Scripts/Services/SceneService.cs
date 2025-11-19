using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Models;
using System.Globalization;
using System;

namespace Services
{
    public class SceneService
    {

        //Método para carregar os objetos da cena a partir do caminho do ficheiro dado
        public void LoadScene(string textContent, out List<ObjectData> sceneObjects, out List<Transformation> transformations, out List<MaterialProperties> materials)
        {
            textContent = textContent.Replace("\r", "");

            sceneObjects = new List<ObjectData>();
            transformations = new List<Transformation>();
            materials = new List<MaterialProperties>();

            string[] lines = textContent.Split('\n');
            int currentLine = 0;

            while (currentLine < lines.Length)
            {
                string line = lines[currentLine].Trim();

                // Saltar linhas vazias
                if (string.IsNullOrWhiteSpace(line))
                {
                    currentLine++;
                    continue;
                }

                // Imagem
                if (line.StartsWith("Image"))
                {
                    currentLine++; // saltar 'Image'
                    currentLine++; // saltar '{'

                    string[] size = lines[currentLine].Trim().Split(' ');
                    int w = int.Parse(size[0]);
                    int h = int.Parse(size[1]);
                    currentLine++;

                    string[] col = lines[currentLine].Trim().Split(' ');
                    float r = float.Parse(col[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(col[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(col[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // saltar '}'

                    sceneObjects.Add(new ImageSettings(w, h, r, g, b));
                    continue;
                }

                // Transformação
                if (line.StartsWith("Transformation"))
                {
                    currentLine++; // saltar 'Transformation'
                    currentLine++; // skip '{'

                    Transformation t = new Transformation(0,0,0,0,0,0,1,1,1);

                    while (!lines[currentLine].Contains("}"))
                    {
                        string[] p = lines[currentLine].Trim().Split(' ');

                        if (p[0] == "T")
                            t.translation = new Vector3(float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture));

                        else if (p[0] == "Rx")
                            t.rotation.x = float.Parse(p[1], CultureInfo.InvariantCulture);

                        else if (p[0] == "Ry")
                            t.rotation.y = float.Parse(p[1], CultureInfo.InvariantCulture);

                        else if (p[0] == "Rz")
                            t.rotation.z = float.Parse(p[1], CultureInfo.InvariantCulture);

                        else if (p[0] == "S")
                        {
                            t.scale = new Vector3(float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture));
                        }

                        currentLine++;
                    }

                    currentLine++; // saltar '}'
                    transformations.Add(t);
                    continue;
                }

                // Material
                if (line.StartsWith("Material"))
                {
                    currentLine++; // saltar "Material"
                    currentLine++; // saltar '{'

                    // primeira linha
                    string[] col = lines[currentLine].Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    float r = float.Parse(col[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(col[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(col[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    // segunda linha
                    string[] props = lines[currentLine].Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    float amb = float.Parse(props[0], CultureInfo.InvariantCulture);
                    float dif = float.Parse(props[1], CultureInfo.InvariantCulture);
                    float spec = float.Parse(props[2], CultureInfo.InvariantCulture);
                    float refr = float.Parse(props[3], CultureInfo.InvariantCulture);
                    float refrI = float.Parse(props[4], CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // saltar '}'

                    materials.Add(new MaterialProperties(r, g, b, amb, dif, spec, refr, refrI));
                    continue;
                }

                // Esfera / Cubo
                if (line.StartsWith("Sphere") || line.StartsWith("Box"))
                {
                    bool isSphere = line.StartsWith("Sphere");

                    currentLine++; // saltar 'Sphere'/'Box'
                    currentLine++; // saltar '{'

                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;

                    int mIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;

                    currentLine++; // saltar '}'

                    if (isSphere)
                        sceneObjects.Add(new SphereData(tIndex, mIndex));
                    else
                        sceneObjects.Add(new BoxData(tIndex, mIndex));

                    continue;
                }

                // Triângulos
                if (line.StartsWith("Triangles"))
                {
                    currentLine++; // saltar 'Triangles'
                    currentLine++; // saltar '{'

                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;

                    while (currentLine < lines.Length &&
                           !lines[currentLine].Trim().StartsWith("}"))
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

                    currentLine++; // saltar '}'
                    continue;
                }

                // Camera
                if (line.StartsWith("Camera"))
                {
                    currentLine++; // saltar 'Camera'
                    currentLine++; // saltar '{'

                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;
                    float dist = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    currentLine++;
                    float fov = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // saltar '}'

                    sceneObjects.Add(new CameraData(tIndex, dist, fov));
                    continue;
                }

                // Luz
                if (line.StartsWith("Light"))
                {
                    currentLine++; // saltar 'Light'
                    currentLine++; // saltar '{'

                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;

                    string[] parts = lines[currentLine].Trim().Split(' ');
                    float r = float.Parse(parts[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // saltar '}'

                    sceneObjects.Add(new LightData(tIndex, r, g, b));
                    continue;
                }

                // Próximo objeto
                currentLine++;
            }
        }
    }
}