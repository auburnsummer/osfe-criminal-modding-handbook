using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Assembly_CSharp
{
    public static class Utils
    {
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

        public static string modFolder = "DataFiles";

        public static string modPath = Path.Combine(Application.persistentDataPath, modFolder);

        /*
         * Return a list of file paths corresponding to the given postfix (e.g. ".png")
         * NB: search RECURSIVELY        
         */
        public static List<string> GetModFiles(string directory, string postfix)
        {
            Debug.Log(String.Format("We're looking for files ending with {0} in {1}", postfix, directory));
            string assetsDirectory = CombinePaths(modPath, "AdditionalAssets", directory);
            /* Make the assets directory if it doesn't exist. */
            DirectoryInfo di = Directory.CreateDirectory(assetsDirectory); // Don't need to check first.
            string[] filenames = Directory.GetFiles(assetsDirectory, "*", SearchOption.AllDirectories);
            List<string> finalList = new List<string>();
            foreach (string path in filenames)
            {
                // .png and .PNG are equally valid
                if (path.EndsWith(postfix, StringComparison.CurrentCultureIgnoreCase))
                {
                    Debug.Log(String.Format("Found {0}", path));
                    finalList.Add(path);
                }
            }
            return finalList;

        }

        public static SpriteAnimationClip LoadNewSpriteAnimationClip(string[] FilePaths, float PixelsPerUnit = 100.0f, float KeyFrameLength = 0.02f)
        {
            Debug.Log("Hi, welcome to LoadNewSpriteAnimationClip!");
            List<Sprite> sprites = new List<Sprite>();
            List<float> numbers = new List<float>();
            for (int i = 0; i < FilePaths.Length; i++)
            {
                numbers.Add((float)i);
                Debug.Log(String.Format("Making a sprite from {0}", FilePaths[i]));
                sprites.Add(LoadNewSprite(FilePaths[i]));
            }
            SpriteAnimationClip newClip = new SpriteAnimationClip(KeyFrameLength, numbers.ToArray(), sprites.ToArray());
            return newClip;
        }

        public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 1.0f)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference

            Sprite NewSprite = new Sprite();
            Texture2D SpriteTexture = LoadTexture(FilePath);
            NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit, 1);

            return NewSprite;
        }

        public static Texture2D LoadTexture(string FilePath)
        {

            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            Texture2D Tex2D;
            byte[] FileData;

            if (File.Exists(FilePath))
            {
                FileData = File.ReadAllBytes(FilePath);
                Tex2D = new Texture2D(40, 40, TextureFormat.RGBA32, false); // Create new "empty" texture
                if (Tex2D.LoadImage(FileData))
                {   // Load the imagedata into the texture (size is set automatically)
                    Tex2D.filterMode = FilterMode.Point;
                    Tex2D.wrapMode = TextureWrapMode.Clamp;
                    Tex2D.anisoLevel = 1;
                    return Tex2D; // If data = readable -> return texture
                }
            }
            return null; // Return null if load failed
        }
    }
}
