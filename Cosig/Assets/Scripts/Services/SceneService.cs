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
        public List<ObjectData> LoadScene(string filePath, out List<ObjectData> sceneObjects, out List<Transformation> transformations, out List<MaterialProperties> materials)
        {
            sceneObjects = new List<ObjectData>();
            transformations = new List<Transformation>();
            materials = new List<MaterialProperties>();
            // Check if the file exists before proceeding
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found at {filePath}");
                return sceneObjects;
            }
            // Read all lines from the configuration file
            string[] lines = File.ReadAllLines(filePath);

            // Process each line to populate sceneObjects list
            foreach (string line in lines)
            {

                if (line.StartsWith("Transformation"))
                {

                }

                else if (line.StartsWith("Material"))
                {

                }

                else if (line.StartsWith("Sphere"))
                {

                }

                else if (line.StartsWith("Box"))
                {

                }

                else if (line.StartsWith("Triangles"))
                {

                }

                else if (line.StartsWith("Camera"))
                {

                }

                else if (line.StartsWith("Light"))
                {

                }
            }
        }
    }
}