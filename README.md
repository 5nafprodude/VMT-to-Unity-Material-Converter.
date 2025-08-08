Unity VMT Material Converter
📄 About
This Unity Editor tool is designed to automate the process of converting Valve Material Type (VMT) files from the Source engine into native Unity materials. It's a huge time-saver for developers migrating assets from Source games.

✨ Features
Automatic Conversion: Converts VMT files to Unity's .mat format.

Texture Mapping: Automatically assigns $basetexture, $bumpmap, and $phongexponenttexture to Unity's Standard shader properties (_MainTex, _BumpMap, and _MetallicGlossMap).

Folder Creation: Creates the necessary folder structure in your Unity project to mirror the location of your VMT files.

Error Logging: Provides a clear log of the conversion process, including any warnings about missing textures.

🛠️ Installation
Create a folder named Editor inside your Unity project's Assets folder (if you don't already have one).

Inside the Editor folder, create another folder called Material Helper.

Copy the provided C# script into this new folder.

⚙️ How to Use
In the Unity Editor, go to Tools > Material Helper to open the tool window.

Click the "Select Folder" button and choose the folder that contains your .vmt files.

Click "Start Conversion". The tool will automatically create .mat files and attempt to assign textures to them.

Check the log for any warnings about textures that couldn't be found.

⚠️ Note on Textures
The tool searches for textures by filename across your entire Unity project. It assumes a basic naming convention. If a texture is not found, it will log a warning, but the material file will still be created for you to manually assign the texture later.

📜 License
This project is licensed under the MIT License.

👏 Acknowledgements
This tool was created to help with the asset migration process from the Source engine. A big thanks to everyone in the modding community for their hard work and dedication.
