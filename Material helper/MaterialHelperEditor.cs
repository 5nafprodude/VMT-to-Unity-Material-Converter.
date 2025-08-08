using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// This class creates a new menu item and a window in the Unity Editor.
public class MaterialHelperEditor : EditorWindow
{
    // The path to the folder containing the VMT files.
    private string vmtFolderPath;

    // UI Elements for our window.
    private Label logLabel;
    private ScrollView logScrollView;
    private TextField vmtFolderPathField;
    private ProgressBar progressBar;
    private Button startButton;

    // A scrollable log to display the tool's output.
    private string logText = "";
    private const int MaxLogLength = 5000;

    // The name of the Unity Standard shader.
    private const string StandardShaderName = "Standard";

    // Adds a menu item named "Material Helper" under the "Tools" menu.
    [MenuItem("Tools/Material Helper")]
    public static void ShowWindow()
    {
        // Creates a new window or brings an existing one to the front.
        MaterialHelperEditor window = GetWindow<MaterialHelperEditor>("Material Helper");
        window.minSize = new Vector2(400, 300);
    }

    // Called when the window is opened. We'll set up the UI here.
    private void OnEnable()
    {
        // Define the UI layout using UXML as a string.
        string uxmlContent = @"
<UXML xmlns:ui='UnityEngine.UIElements'>
  <ui:VisualElement style='flex-grow: 1; padding: 10px;'>
    <ui:Label text='VMT to Unity Material Converter' style='font-size: 20px; -unity-font-style: bold; margin-bottom: 15px;' />
    <ui:Label text='Select VMT Folder:' />
    <ui:VisualElement style='flex-direction: row; margin-bottom: 10px;'>
      <ui:TextField name='vmt-path-field' label='' style='flex-grow: 1;' />
      <ui:Button name='select-folder-button' text='Select Folder' style='margin-left: 5px;' />
    </ui:VisualElement>
    <ui:Button name='start-conversion-button' text='Start Conversion' style='margin-bottom: 10px;' />
    <ui:VisualElement style='flex-direction: row; margin-bottom: 10px;'>
      <ui:ProgressBar name='progress-bar' title='Progress' style='flex-grow: 1;' />
      <ui:Button name='clear-log-button' text='Clear Log' style='margin-left: 5px;' />
    </ui:VisualElement>
    <ui:Label text='Log:' style='-unity-font-style: bold;' />
    <ui:ScrollView name='log-scroll-view' style='flex-grow: 1; border-color: #555; border-width: 1px; padding: 5px; background-color: #333;'>
      <ui:Label name='log-label' text='' style='white-space: pre-wrap; color: #aaa;'/>
    </ui:ScrollView>
  </ui:VisualElement>
</UXML>
";

        // Define the UI styles using USS as a string.
        string ussContent = @"
.unity-button {
    background-color: #4CAF50;
    color: white;
    border-radius: 5px;
}
.unity-button:hover {
    background-color: #45a049;
}
.unity-button:disabled {
    background-color: #888;
    color: #bbb;
}
.unity-text-field__input {
    background-color: #444;
    color: #ccc;
    border-radius: 5px;
}
.unity-label {
    color: #eee;
}
.unity-progress-bar__title {
    color: #ccc;
}
.unity-progress-bar__container {
    background-color: #444;
    border-radius: 5px;
    height: 25px;
}
.unity-progress-bar__progress {
    background-color: #4CAF50;
    border-radius: 5px;
}
";
        
        // The path to the UXML and USS files within the project.
        string uxmlPath = "Assets/Editor/Material Helper/MaterialHelperEditor.uxml";
        string ussPath = "Assets/Editor/Material Helper/MaterialHelperEditor.uss";

        // Check if the UXML file exists, if not, create it.
        if (!File.Exists(uxmlPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(uxmlPath));
            File.WriteAllText(uxmlPath, uxmlContent);
            File.WriteAllText(ussPath, ussContent);
            AssetDatabase.ImportAsset(uxmlPath);
            AssetDatabase.ImportAsset(ussPath);
        }

        // Now that we're sure the files exist, we can find and load the VisualTreeAsset.
        string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { Path.GetDirectoryName(uxmlPath) });

        VisualTreeAsset visualTree = null;
        if (guids.Length > 0)
        {
            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        else
        {
            Debug.LogError("Failed to find UXML file. Please ensure the file exists at " + uxmlPath);
            return;
        }

        visualTree.CloneTree(rootVisualElement);

        // Find the UI elements by name.
        vmtFolderPathField = rootVisualElement.Q<TextField>("vmt-path-field");
        Button selectFolderButton = rootVisualElement.Q<Button>("select-folder-button");
        startButton = rootVisualElement.Q<Button>("start-conversion-button");
        Button clearLogButton = rootVisualElement.Q<Button>("clear-log-button");
        logLabel = rootVisualElement.Q<Label>("log-label");
        // Get a direct reference to the ScrollView.
        logScrollView = rootVisualElement.Q<ScrollView>("log-scroll-view");
        progressBar = rootVisualElement.Q<ProgressBar>("progress-bar");

        // Hook up event handlers.
        selectFolderButton.clicked += SelectFolder;
        startButton.clicked += StartConversion;
        clearLogButton.clicked += ClearLog;

        // Sync the text field with the serialized property.
        if (!string.IsNullOrEmpty(vmtFolderPath))
        {
            vmtFolderPathField.value = vmtFolderPath;
        }
        vmtFolderPathField.RegisterValueChangedCallback(evt => {
            vmtFolderPath = evt.newValue;
            UpdateUIState();
        });

        // Initialize UI state.
        UpdateLog("Welcome to the VMT to Unity Material Converter!");
        UpdateUIState();
    }
    
    // Updates the state of the UI elements (e.g., enable/disable buttons).
    private void UpdateUIState()
    {
        bool folderExists = !string.IsNullOrEmpty(vmtFolderPath) && Directory.Exists(vmtFolderPath);
        startButton.SetEnabled(folderExists);
        progressBar.value = 0;
        progressBar.title = "Progress";
    }

    // Opens a file dialog to let the user select a folder.
    private void SelectFolder()
    {
        string path = EditorUtility.OpenFolderPanel("Select VMT Folder", "", "");
        if (!string.IsNullOrEmpty(path))
        {
            vmtFolderPath = path;
            vmtFolderPathField.value = vmtFolderPath;
            UpdateLog("Folder selected: " + vmtFolderPath);
        }
        UpdateUIState();
    }

    // The main function that starts the conversion process.
    private async void StartConversion()
    {
        startButton.SetEnabled(false);
        UpdateLog("Starting material conversion...");

        // Find all .vmt files in the selected directory and subdirectories.
        string[] vmtFiles = Directory.GetFiles(vmtFolderPath, "*.vmt", SearchOption.AllDirectories);
        if (vmtFiles.Length == 0)
        {
            UpdateLog("No .vmt files found in the selected folder.");
            EditorUtility.DisplayDialog("Error", "No .vmt files found in the selected folder.", "OK");
            startButton.SetEnabled(true);
            return;
        }

        UpdateLog($"Found {vmtFiles.Length} VMT files to process.");
        
        float progressStep = 100f / vmtFiles.Length;
        
        for (int i = 0; i < vmtFiles.Length; i++)
        {
            string vmtFilePath = vmtFiles[i];
            ProcessVmtFile(vmtFilePath);
            
            // Update the progress bar.
            progressBar.value = (i + 1) * progressStep;
            progressBar.title = $"Progress: {i + 1}/{vmtFiles.Length}";
            await Task.Delay(10);
        }

        UpdateLog("\nMaterial conversion complete!");
        EditorUtility.DisplayDialog("Success", "Material conversion complete!", "OK");
        AssetDatabase.Refresh(); // Refresh the AssetDatabase to show new/updated files.
        startButton.SetEnabled(true);
    }

    // Processes a single VMT file to create or update a Unity material.
    private void ProcessVmtFile(string vmtFilePath)
    {
        string vmtFileName = Path.GetFileNameWithoutExtension(vmtFilePath);
        UpdateLog($"\nProcessing material: {vmtFileName}");

        // Parse the VMT file to find texture paths.
        Dictionary<string, string> texturePaths = ParseVmtFile(vmtFilePath);

        if (texturePaths.Count == 0)
        {
            UpdateLog("No textures found in VMT file. Skipping.");
            return;
        }

        // Get the path for the new .mat file relative to the Assets folder.
        // This is necessary for AssetDatabase operations.
        string vmtFilePathRelativeToAssets = "Assets" + vmtFilePath.Substring(Application.dataPath.Length);
        string materialPath = Path.ChangeExtension(vmtFilePathRelativeToAssets, ".mat").Replace("\\", "/");

        // Get the parent directory's full path, which Directory.CreateDirectory needs.
        string parentDirectoryFullPath = Path.GetDirectoryName(vmtFilePath);

        // Check if a material already exists at the path.
        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Material material;

        if (existingMaterial != null)
        {
            // If the material exists, use it.
            material = existingMaterial;
            UpdateLog("  - Material file already exists. Updating.");
        }
        else
        {
            // Ensure the parent directory exists before creating the asset.
            if (!Directory.Exists(parentDirectoryFullPath))
            {
                Directory.CreateDirectory(parentDirectoryFullPath);
                UpdateLog($"  - Created directory: {parentDirectoryFullPath}");
            }
            
            // If it doesn't exist, create a new one.
            // Find the Standard shader.
            Shader standardShader = Shader.Find(StandardShaderName);
            if (standardShader == null)
            {
                UpdateLog($"  - ERROR: Could not find the '{StandardShaderName}' shader. Skipping.");
                return;
            }
            material = new Material(standardShader);
            UpdateLog("  - Creating new material file.");
        }

        // Find and assign textures.
        AssignTexture(material, texturePaths, "albedo", "_MainTex");
        AssignTexture(material, texturePaths, "normal", "_BumpMap");
        AssignTexture(material, texturePaths, "metallic", "_MetallicGlossMap");

        // Save the material to a file.
        if (existingMaterial == null)
        {
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }
    }

    // A helper method to parse the VMT file using regular expressions.
    private Dictionary<string, string> ParseVmtFile(string filePath)
    {
        var textures = new Dictionary<string, string>();
        try
        {
            string content = File.ReadAllText(filePath);

            // Regex to find texture paths for different maps.
            // Note: VMT paths are often lowercase and can use backslashes.
            Match albedoMatch = Regex.Match(content, @"\$basetexture""\s+""(.*?)\""", RegexOptions.IgnoreCase);
            if (albedoMatch.Success) textures["albedo"] = albedoMatch.Groups[1].Value;

            Match normalMatch = Regex.Match(content, @"\$bumpmap""\s+""(.*?)\""", RegexOptions.IgnoreCase);
            if (normalMatch.Success) textures["normal"] = normalMatch.Groups[1].Value;

            Match metallicMatch = Regex.Match(content, @"\$phongexponenttexture""\s+""(.*?)\""", RegexOptions.IgnoreCase);
            if (metallicMatch.Success) textures["metallic"] = metallicMatch.Groups[1].Value;
        }
        catch (System.Exception e)
        {
            UpdateLog($"  - ERROR parsing VMT file: {e.Message}");
        }

        return textures;
    }

    // A helper method to find a texture and assign it to a material property.
    private void AssignTexture(Material material, Dictionary<string, string> texturePaths, string key, string propertyName)
    {
        if (texturePaths.ContainsKey(key))
        {
            // Get the original texture path from the VMT.
            string originalTexturePath = texturePaths[key];

            // Check if the texture is a VTF and try to find a converted PNG.
            string textureToFind = originalTexturePath;
            if (originalTexturePath.ToLower().EndsWith(".vtf"))
            {
                // The script expects the user to have already converted VTF to PNG.
                // It now just changes the extension and looks for the converted file.
                string pngPath = originalTexturePath.Replace(".vtf", ".png");
                textureToFind = pngPath;
                UpdateLog($"  - VMT specified a VTF texture. Looking for a converted PNG at '{pngPath}'.");
            }
            
            string textureName = Path.GetFileNameWithoutExtension(textureToFind);
            
            // Search the entire project for a texture with a matching name.
            string[] guids = AssetDatabase.FindAssets($"{textureName} t:texture");

            if (guids.Length > 0)
            {
                string texturePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                
                if (texture != null)
                {
                    material.SetTexture(propertyName, texture);
                    UpdateLog($"  - Assigned '{Path.GetFileName(texturePath)}' to '{propertyName}'.");
                    return; // Texture found and assigned, no need to continue.
                }
            }
            
            // If the texture was not found, log a warning.
            UpdateLog($"  - WARNING: Texture for '{propertyName}' not found in the project. (Expected: {textureToFind})");
        }
    }

    // A helper method to append messages to the log text area.
    private void UpdateLog(string message)
    {
        logText += message + "\n";
        // Ensure the log text doesn't get too large.
        if (logText.Length > MaxLogLength)
        {
            logText = logText.Substring(logText.Length - MaxLogLength);
        }
        logLabel.text = logText;
        logScrollView.schedule.Execute(() => {
            logScrollView.scrollOffset = new Vector2(0, 9999);
        });
    }
    
    // A method to clear the log.
    private void ClearLog()
    {
        logText = "";
        logLabel.text = logText;
    }
}
