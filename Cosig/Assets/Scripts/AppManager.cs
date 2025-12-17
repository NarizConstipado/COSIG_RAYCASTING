using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;

[RequireComponent(typeof(SceneBuilder))]
#endif

public class AppManager : MonoBehaviour
{
    private SceneBuilder sceneBuilder;
    
    [Header("UI - Image Settings")]
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private TMP_InputField rInput;
    [SerializeField] private TMP_InputField gInput;
    [SerializeField] private TMP_InputField bInput;
    [SerializeField] private Slider rSlider;
    [SerializeField] private Slider gSlider;
    [SerializeField] private Slider bSlider;
    [Header("")]
    [SerializeField] private TMP_InputField recursionInput;

    [Header("UI - Lighting Controls")]
    [SerializeField] private Toggle ambientToggle;
    [SerializeField] private Toggle specularToggle;
    [SerializeField] private Toggle diffuseToggle;
    [SerializeField] private Toggle refractionToggle;

    [Header("UI - Camera Controls")]
    [SerializeField] private TMP_InputField FOVInput;
    [SerializeField] private TMP_InputField xInput;
    [SerializeField] private TMP_InputField yInput;
    [SerializeField] private TMP_InputField zInput;
    [SerializeField] private TMP_InputField xRotation;
    [SerializeField] private TMP_InputField yRotation;
    [SerializeField] private TMP_InputField zRotation;
    [SerializeField] private Slider xRotSlider;
    [SerializeField] private Slider yRotSlider;
    [SerializeField] private Slider zRotSlider;

    [Header("Other Settings")]
    [SerializeField] private GameObject progressSlider;
    [SerializeField] private TMP_Text elapsedTime;
    [SerializeField] private TMP_Text progress;

    [SerializeField] private RawImage loadImage;
    
    void Awake()
    {
        sceneBuilder = GetComponent<SceneBuilder>();
    }

    void Start()
    {
        sceneBuilder.OnSceneLoaded += PopulateUIFromScene;
    }
    public void OnLoadScene()
    {
        #if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel(
                "Load Scene",
                Application.dataPath,
                "txt,json"
            );

            if (string.IsNullOrEmpty(path))
                return;

            sceneBuilder.LoadSceneFromPath(path);

            Debug.Log("Scene loaded: " + path);
        #else
            Debug.LogWarning("File browser only works in Editor.");
        #endif
    }
    public void OnLoadImage()
    {
        #if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel(
                "Load Image",
                Application.dataPath,
                "png,jpg,jpeg"
            );

            if (string.IsNullOrEmpty(path))
                return;

            byte[] fileData = File.ReadAllBytes(path);

            Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(fileData);
            tex.Apply();

            // Mostrar na RawImage
            loadImage.texture = tex;

            Debug.Log("Image loaded: " + path);
        #else
            Debug.LogWarning("File browser only works in Editor.");
        #endif
    }
    public void OnStartRender() { sceneBuilder.RenderGPU(); elapsedTime.text = $"Elapsed time: {sceneBuilder.GetElapsedTime()} ms";
    }
    public void OnRecursionChanged() { if (int.TryParse(recursionInput.text, out int rec)) sceneBuilder.SetRecursionDepth(rec); }

    //Image Settings
    public void OnWidthChanged() { if (int.TryParse(widthInput.text, out int w)) sceneBuilder.SetImageWidth(w); }
    public void OnHeightChanged()
    {
        if (int.TryParse(heightInput.text, out int h))
            sceneBuilder.SetImageHeight(h);
    }   

    public void OnColorChanged_R()
    {
        if (int.TryParse(rInput.text, out int r))
            sceneBuilder.SetBackgroundColorR(r);
    }
    public void OnColorChanged_G()
    {
        if (int.TryParse(gInput.text, out int g))
            sceneBuilder.SetBackgroundColorG(g);
    }
    public void OnColorChanged_B()
    {
        if (int.TryParse(bInput.text, out int b))
            sceneBuilder.SetBackgroundColorB(b);
    }
    public void OnSliderChanged_R()
    {
        int value = Mathf.RoundToInt(rSlider.value);
        rInput.text = value.ToString();
        sceneBuilder.SetBackgroundColorR(value);
    }
    public void OnSliderChanged_G()
    {
        int value = Mathf.RoundToInt(gSlider.value);
        gInput.text = value.ToString();
        sceneBuilder.SetBackgroundColorG(value);
    }
    public void OnSliderChanged_B()
    {
        int value = Mathf.RoundToInt(bSlider.value);
        bInput.text = value.ToString();
        sceneBuilder.SetBackgroundColorB(value);
    }

    //Lighting Settings
    public void OnAmbient() { sceneBuilder.SetAmbient(ambientToggle.isOn); }
    public void OnSpecular() { sceneBuilder.SetSpecular(specularToggle.isOn); }
    public void OnDiffuse() { sceneBuilder.SetDiffuse(diffuseToggle.isOn); }
    public void OnRefraction() { sceneBuilder.SetRefraction(refractionToggle.isOn); }

    //Camera Controls
    public void OnFOVChanged()
    {
        if (int.TryParse(FOVInput.text, out int fov))
            sceneBuilder.SetCameraFOV(fov);
    }

    public void OnPositionChanged_X()
    {
        if (float.TryParse(xInput.text, out float v))
            sceneBuilder.SetCameraPositionX(v);
    }
    public void OnPositionChanged_Y()
    {
        if (float.TryParse(yInput.text, out float v))
            sceneBuilder.SetCameraPositionY(v);
    }
    public void OnPositionChanged_Z()
    {
        if (float.TryParse(zInput.text, out float v))
            sceneBuilder.SetCameraPositionZ(v);
    }
    public void OnRotationChanged_X()
    {
        if (float.TryParse(xRotation.text, out float v))
            sceneBuilder.SetCameraRotationX(v);
    }
    public void OnRotationChanged_Y()
    {
        if (float.TryParse(yRotation.text, out float v))
            sceneBuilder.SetCameraRotationY(v);
    }
    public void OnRotationChanged_Z()
    {
        if (float.TryParse(zRotation.text, out float v))
            sceneBuilder.SetCameraRotationZ(v);
    }

    public void OnSliderRotation_X()
    {
        xRotation.text = xRotSlider.value.ToString();
        sceneBuilder.SetCameraRotationX(xRotSlider.value);
    }
    public void OnSliderRotation_Y()
    {
        yRotation.text = yRotSlider.value.ToString();
        sceneBuilder.SetCameraRotationY(yRotSlider.value);
    }
    public void OnSliderRotation_Z()
    {
        zRotation.text = zRotSlider.value.ToString();
        sceneBuilder.SetCameraRotationZ(zRotSlider.value);
    }

    public void OnSaveImage()
    {
        string path = Application.dataPath + "/RenderedImage.png";
        sceneBuilder.SaveCurrentImage(path);
    }
    public void OnSaveScene()
    {
        string path = Application.dataPath + "/SavedScene.json";
        sceneBuilder.SaveSceneToJson(path);
    }
    public void OnExit() 
    {
        Application.Quit();
    }

    void PopulateUIFromScene()
    {
        recursionInput.text = sceneBuilder.GetRecursionDepth().ToString();

        // Image Settings
        widthInput.text = sceneBuilder.GetImageSettingsW().ToString();
        heightInput.text = sceneBuilder.GetImageSettingsY().ToString();

        rInput.text = Mathf.RoundToInt(sceneBuilder.GetBackgroundColorR() * 255f).ToString();
        gInput.text = Mathf.RoundToInt(sceneBuilder.GetBackgroundColorG() * 255f).ToString();
        bInput.text = Mathf.RoundToInt(sceneBuilder.GetBackgroundColorB() * 255f).ToString();

        rSlider.value = sceneBuilder.GetBackgroundColorR() * 255f;
        gSlider.value = sceneBuilder.GetBackgroundColorG() * 255f;
        bSlider.value = sceneBuilder.GetBackgroundColorB() * 255f;

        // Lighting Settings
        ambientToggle.isOn = sceneBuilder.GetAmbient();
        diffuseToggle.isOn = sceneBuilder.GetDiffuse();
        specularToggle.isOn = sceneBuilder.GetSpecular();
        refractionToggle.isOn = sceneBuilder.GetRefraction();

        // Camera Settings
        FOVInput.text = sceneBuilder.GetCameraFOV().ToString();

        xInput.text = sceneBuilder.GetCameraPositionX().ToString();
        yInput.text = sceneBuilder.GetCameraPositionY().ToString();
        zInput.text = sceneBuilder.GetCameraPositionZ().ToString();

        xRotation.text = sceneBuilder.GetCameraRotationX().ToString();
        yRotation.text = sceneBuilder.GetCameraRotationY().ToString();
        zRotation.text = sceneBuilder.GetCameraRotationZ().ToString();
        xRotSlider.value = sceneBuilder.GetCameraRotationX();
        yRotSlider.value = sceneBuilder.GetCameraRotationY();
        zRotSlider.value = sceneBuilder.GetCameraRotationZ();
    }
}
