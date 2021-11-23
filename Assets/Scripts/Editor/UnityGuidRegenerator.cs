using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UnityGuidRegenerator {

    public class UnityGuidRegeneratorMenu
    {
        [MenuItem("Assets/Regenerate GUIDs")]
        public static void RegenerateGuids()
        {
            string assetsPath = Path.Combine(Path.GetFullPath("."), "Assets");
            string filterPath = "";
            switch(EditorUtility.DisplayDialogComplex("Regeneration type", "Regenerate folder or file ?", "File", "Cancel", "Folder"))
            {
                case 0: // File
                    filterPath = EditorUtility.OpenFilePanel("Choose asset to regenerate", assetsPath, "").Replace("/", "\\");
                    break;
                case 2: // Folder
                    filterPath = EditorUtility.OpenFolderPanel("Choose folder to regenerate", assetsPath, "").Replace("/", "\\");
                    break;
                case 1: // Cancel
                    break;
            }
            
            try {
                AssetDatabase.StartAssetEditing();
                    
                if (!filterPath.StartsWith(assetsPath))
                {
                    Debug.LogError($"Path must be a subpath of the Assets folder\nFilterPath: {filterPath}\nAssetsPath: {assetsPath}");
                    return;
                }

                Debug.Log($"FilterPath: {filterPath}");

                UnityGuidRegenerator regenerator = new UnityGuidRegenerator(assetsPath);
                regenerator.RegenerateGuids(filterPath);
            }
            finally {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }
    }

    internal class UnityGuidRegenerator
    {
        private static readonly string[] kDefaultFileExtensions = {
            "*.meta",
            "*.mat",
            "*.anim",
            "*.prefab",
            "*.unity",
            "*.asset"
        };

        private readonly string _assetsPath;

        public UnityGuidRegenerator(string assetsPath) {
            _assetsPath = assetsPath;
        }

        public void RegenerateGuids(string filterPath, string[] regeneratedExtensions = null) {
            if (regeneratedExtensions == null) {
                regeneratedExtensions = kDefaultFileExtensions;
            }

            // Get list of working files
            List<string> filesPaths = new List<string>();
            foreach (string extension in regeneratedExtensions) {
                filesPaths.AddRange(
                    Directory.GetFiles(_assetsPath, extension, SearchOption.AllDirectories)
                    );
            }

            // Create dictionary to hold old-to-new GUID map
            Dictionary<string, string> guidOldToNewMap = new Dictionary<string, string>();
            Dictionary<string, List<string>> guidsInFileMap = new Dictionary<string, List<string>>();

            // We must only replace GUIDs for Resources present in Assets. 
            // Otherwise built-in resources (shader, meshes etc) get overwritten.
            HashSet<string> ownGuids = new HashSet<string>();

            // Traverse all files, remember which GUIDs are in which files and generate new GUIDs
            int counter = 0;
            int numNewGuids = 0;
            foreach (string filePath in filesPaths) {
                EditorUtility.DisplayProgressBar("Scanning Assets folder", MakeRelativePath(_assetsPath, filePath), counter / (float) filesPaths.Count);
                string contents = File.ReadAllText(filePath);
                
                IEnumerable<string> guids = GetGuids(contents);
                bool isFirstGuid = true;
                foreach (string oldGuid in guids) {
                    // First GUID in .meta file is always the GUID of the asset itself
                    if (isFirstGuid && Path.GetExtension(filePath) == ".meta") {
                        ownGuids.Add(oldGuid);
                        isFirstGuid = false;
                    }

                    // Generate and save new GUID if we haven't added it before
                    if (!guidOldToNewMap.ContainsKey(oldGuid)) {
                        if (filterPath == "" || filePath.StartsWith(filterPath))
                        {
                            string newGuid = Guid.NewGuid().ToString("N");
                            guidOldToNewMap.Add(oldGuid, newGuid);
                            numNewGuids++;

                            Debug.Log($"{filePath}: {oldGuid} -> {newGuid}");
                        }
                        else
                        {
                            guidOldToNewMap.Add(oldGuid, oldGuid);
                        }
                    }

                    if (!guidsInFileMap.ContainsKey(filePath))
                        guidsInFileMap[filePath] = new List<string>();

                    if (!guidsInFileMap[filePath].Contains(oldGuid)) {
                        guidsInFileMap[filePath].Add(oldGuid);
                    }
                }

                counter++;
            }
            
            EditorUtility.ClearProgressBar();
            if (!EditorUtility.DisplayDialog("GUIDs regeneration",
                $"You are going to start the process of GUID regeneration.\n{numNewGuids} assets will have their guid changed.\nSee info logs for detailed breakdown.\nThis may have unexpected results.\n\n MAKE A PROJECT BACKUP BEFORE PROCEEDING!",
                "Regenerate GUIDs", "Cancel")) return;

            // Traverse the files again and replace the old GUIDs
            counter = -1;
            int numModified = 0;
            int guidsInFileMapKeysCount = guidsInFileMap.Keys.Count;
            foreach (string filePath in guidsInFileMap.Keys)
            {
                EditorUtility.DisplayProgressBar("Regenerating GUIDs", MakeRelativePath(_assetsPath, filePath), counter / (float) guidsInFileMapKeysCount);
                counter++;

                string contents = File.ReadAllText(filePath);
                bool modified = false;
                foreach (string oldGuid in guidsInFileMap[filePath])
                {
                    if (!ownGuids.Contains(oldGuid))
                        continue;

                    string newGuid = guidOldToNewMap[oldGuid];
                    if (string.IsNullOrEmpty(newGuid)) throw new NullReferenceException($"newGuid == null\nFilePath: {filePath}\nGUID: {oldGuid}");
                    if (newGuid == oldGuid) continue;

                    contents = contents.Replace("guid: " + oldGuid, "guid: " + newGuid);
                    modified = true;
                }

                if (modified)
                {
                    File.WriteAllText(filePath, contents);
                    numModified++;
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"Modified {numModified} assets.");
        }

        private static IEnumerable<string> GetGuids(string text)
        {
            const string guidStart = "guid: ";
            const int guidLength = 32;
            int textLength = text.Length;
            int guidStartLength = guidStart.Length;
            List<string> guids = new List<string>();

            int index = 0;
            while (index + guidStartLength + guidLength < textLength) {
                index = text.IndexOf(guidStart, index, StringComparison.Ordinal);
                if (index == -1)
                    break;

                index += guidStartLength;
                string guid = text.Substring(index, guidLength);
                index += guidLength;

                if (IsGuid(guid)) {
                    guids.Add(guid);
                }
            }

            return guids;
        }

        private static bool IsGuid(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (
                    !((c >= '0' && c <= '9') ||
                      (c >= 'a' && c <= 'z'))
                    )
                    return false;
            }

            return true;
        }

        private static string MakeRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }
    }
}