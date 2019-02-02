using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Assembly_CSharp
{
    /* PathMan.EXE
     * 1400 HP
     * Provides functions that handle folders, directories, etc.
     */
    public static class PathMan
    {
        public static string BASE_DIRECTORY = Application.persistentDataPath;
        public static string MOD_DIRECTORY = "DataFiles";
        public static string ASSETS_DIRECTORY = "AdditionalAssets";
        public static string ANIM_CLIPS_DIRECTORY = "SpriteAnimationCLips";
        public static string ICONS_DIRECTORY = "SpellIcons";

        // check out this indentation, i'm indenting according to the directory structure
        // inb4 codehorror
        public static string MOD_PATH = CombinePaths(BASE_DIRECTORY, MOD_DIRECTORY);
        /**/public static string ASSETS_PATH = CombinePaths(MOD_PATH, ASSETS_DIRECTORY);
        /*    */public static string ICONS_PATH = CombinePaths(ASSETS_PATH, ICONS_DIRECTORY);
        /*    */public static string ANIM_CLIPS_PATH = CombinePaths(ASSETS_PATH, ANIM_CLIPS_DIRECTORY);


        /* Combines together a list of paths. */
        public static string CombinePaths(params string[] paths)
        {
            if (paths == null)
            {
                return null;
            }
            string currentPath = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                currentPath = Path.Combine(currentPath, paths[i]);
            }
            return currentPath;
        }

        /* Search a specified directory for files matching a postfix. */
        public static List<string> GetModFiles(string dir, string postfix, bool exactMatch = false)
        {
            Debug.Log(String.Format("We're looking for files ending with {0} in {1}", postfix, dir));

            /* Make the assets directory if it doesn't exist. */
            DirectoryInfo di = Directory.CreateDirectory(dir); // Don't need to check first.

            string[] filenames = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);

            // Filter for files that match the postfix criteria.
            List<string> finalList = new List<string>();

            foreach (string path in filenames)
            {
                // If we're looking for an exact match, there can only be one file that matches the criteria.
                if (exactMatch)
                {
                    if (Path.GetFileName(path) == postfix)
                    {
                        Debug.Log(String.Format("Found {0} (exact match)", path));
                        finalList.Add(path);
                        break;
                    }
                }
                // .png and .PNG are equally valid
                else if (path.EndsWith(postfix, StringComparison.CurrentCultureIgnoreCase))
                {
                    Debug.Log(String.Format("Found {0}", path));
                    finalList.Add(path);
                }
            }
            return finalList;
        }


        /*
         * Load a single file from the specified path
         * And if the file doesn't exist, it will take the embedded resource resourceID and write it out first.
         * So it's like the ingame XML files, except we can define what they are initially.        
         */
        public static string GetModInternalFile(string directory, string filename, string resourceID)
        {
            List<string> initialModList = GetModFiles(directory, filename, true);
            string targetPath = CombinePaths(directory, filename);

            if (initialModList.Count == 0)
            {
                // It's not in the mod directory yet, so load it from our internal resources.
                
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceID);
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(targetPath, result);
                    initialModList.Add(targetPath);
                }
            }
            // This will either be the thing we just wrote out, or one already written.
            return File.ReadAllText(initialModList[0]);
        }

    }
}
