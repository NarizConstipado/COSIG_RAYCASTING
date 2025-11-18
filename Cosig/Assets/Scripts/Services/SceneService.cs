// Services/SceneService.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Models;
using System.Globalization;
using System;

namespace Services
{
    // Service responsible for loading and interpreting data from a scene configuration file
    public class SceneService
    {
        // Method to load scene objects from a given configuration file path
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

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    currentLine++;
                    continue;
                }

                // --- IMAGE ---
                if (line.StartsWith("Image"))
                {
                    currentLine++; // skip 'Image'
                    currentLine++; // skip '{'

                    string[] size = lines[currentLine].Trim().Split(' ');
                    int w = int.Parse(size[0]);
                    int h = int.Parse(size[1]);
                    currentLine++;

                    string[] col = lines[currentLine].Trim().Split(' ');
                    float r = float.Parse(col[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(col[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(col[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // skip '}'

                    sceneObjects.Add(new ImageSettings(w, h, r, g, b));
                    continue;
                }

                // --- TRANSFORMATION ---
                if (line.StartsWith("Transformation"))
                {
                    currentLine++; // skip header
                    currentLine++; // skip '{'

                    Transformation t = new Transformation(0,0,0,0,0,0,0,0,0);

                    while (!lines[currentLine].Contains("}"))
                    {
                        string[] p = lines[currentLine].Trim().Split(' ');

                        if (p[0] == "T")
                            t.translation = new Vector3(float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));

                        else if (p[0] == "Rx")
                            t.rotation.x = float.Parse(p[1]);

                        else if (p[0] == "Ry")
                            t.rotation.y = float.Parse(p[1]);

                        else if (p[0] == "Rz")
                            t.rotation.z = float.Parse(p[1]);

                        else if (p[0] == "S")
                            t.scale = new Vector3(float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));

                        currentLine++;
                    }

                    currentLine++; // skip '}'
                    transformations.Add(t);
                    continue;
                }

                // --- MATERIAL ---
                if (line.StartsWith("Material"))
                {
                    currentLine++; // skip header
                    currentLine++; // skip '{'

                    // first line
                    string[] col = lines[currentLine].Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    float r = float.Parse(col[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(col[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(col[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    // second line
                    string[] props = lines[currentLine].Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    float amb = float.Parse(props[0], CultureInfo.InvariantCulture);
                    float dif = float.Parse(props[1], CultureInfo.InvariantCulture);
                    float spec = float.Parse(props[2], CultureInfo.InvariantCulture);
                    float refr = float.Parse(props[3], CultureInfo.InvariantCulture);
                    float refrI = float.Parse(props[4], CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // skip '}'

                    materials.Add(new MaterialProperties(r, g, b, amb, dif, spec, refr, refrI));
                    continue;
                }

                // --- SPHERE / BOX ---
                if (line.StartsWith("Sphere") || line.StartsWith("Box"))
                {
                    bool isSphere = line.StartsWith("Sphere");

                    currentLine++; // skip header
                    currentLine++; // skip '{'

                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;

                    int mIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;

                    currentLine++; // skip '}'

                    if (isSphere)
                        sceneObjects.Add(new SphereData(tIndex, mIndex));
                    else
                        sceneObjects.Add(new BoxData(tIndex, mIndex));

                    continue;
                }

                // --- TRIANGLES ---
                if (line.StartsWith("Triangles"))
                {
                    currentLine++; // skip "Triangles"
                    currentLine++; // skip "{"

                    // Read transformation index
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

                    currentLine++; // skip "}"
                    continue;
                }

                // --- CAMERA ---
                if (line.StartsWith("Camera"))
                {
                    currentLine++; // skip header
                    currentLine++; // skip '{'

                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;
                    float dist = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    currentLine++;
                    float fov = float.Parse(lines[currentLine].Trim(), CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // skip '}'

                    sceneObjects.Add(new CameraData(tIndex, dist, fov));
                    continue;
                }

                // --- LIGHT ---
                if (line.StartsWith("Light"))
                {
                    currentLine++; // skip header
                    currentLine++; // skip '{'

                    int tIndex = int.Parse(lines[currentLine].Trim());
                    currentLine++;

                    string[] parts = lines[currentLine].Trim().Split(' ');
                    float r = float.Parse(parts[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    currentLine++;

                    currentLine++; // skip '}'

                    sceneObjects.Add(new LightData(tIndex, r, g, b));
                    continue;
                }

                // Default: next line
                currentLine++;
            }
        }
    }
}