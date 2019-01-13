using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MonoMod;
using UnityEngine;
using Assembly_CSharp;
using System.Runtime.InteropServices;
#pragma warning disable CS0626
namespace Assembly_CSharp
{
    public static class Utils
    {
        public static string modFolder = "DataFiles";

        public static string modPath = Path.Combine(Application.persistentDataPath, modFolder);

        /*
         * Return a list of file paths corresponding to the given postfix (e.g. ".png")
         */
        public static List<string> GetModFiles(string postfix)
        {
            Debug.Log(String.Format("We're looking for files ending with {0}", postfix));
            string assetsDirectory = Path.Combine(modPath, "AdditionalAssets");
            /* Make the assets directory if it doesn't exist. */
            DirectoryInfo di = Directory.CreateDirectory(assetsDirectory); // Don't need to check first.
            string[] filenames = Directory.GetFiles(assetsDirectory, "*", SearchOption.AllDirectories);
            List<string> finalList = new List<string>();
            foreach (string path in filenames)
            {
                // .png and .PNG are equally valid
                if (path.EndsWith(postfix, StringComparison.CurrentCultureIgnoreCase))
                {
                    Debug.Log(String.Format("Loading additional data from {0}", path));
                    finalList.Add(path);
                }
            }
            return finalList;

        }

        public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f)
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

    [MonoModPatch("global::ItemManager")]
    public class patch_ItemManager : ItemManager
    {
        [MonoModPublic]
        [MonoModIgnore]
        public extern void ReadSpellFile(string xmlString);

        public extern void orig_Awake();
        public void Awake()
        {
            Debug.Log("Hi from MonoMod on Mac Version 2!");
            orig_Awake();
            /* Load additional sprites. */
            foreach (string path in Utils.GetModFiles(".png"))
            {
                Debug.Log(String.Format("Okay, I'm going to try and load {0}", path));
                Sprite new_sprite = Utils.LoadNewSprite(path);
                this.sprites[Path.GetFileNameWithoutExtension(path)] = new_sprite;
            }
        }
        /*
        public extern SpriteAnimationClip orig_GetClip(string clipName);
        public new SpriteAnimationClip GetClip(string clipName)
        {
            Debug.Log(String.Format("Getting SpriteAnimationClip {0}", clipName));
            return orig_GetClip(clipName);
        }

        public extern RuntimeAnimatorController orig_GetAnim(string animName);
        public new RuntimeAnimatorController GetAnim(string animName)
        {
            Debug.Log(String.Format("Getting Anim {0}", animName));
            return orig_GetAnim(animName);
        }
        */
    }

    [MonoModPatch("global::Effect")]
    public enum patch_Effect
    {
        // thanks 0x0ade for your help!
        Custom = 0x0ade
    }

    [MonoModPatch("global::SpellObject")]
    public class patch_SpellObject
    {
        [MonoModAdded]
        public void Log(string value)
        {
            Debug.Log(value);
        }
    }

    [MonoModPatch("global::XMLReader")]
    public class patch_XMLReader
    {
        public extern string orig_GetDataFile(string dataFile);
        public string GetDataFile(string dataFile)
        {
            Debug.Log(String.Format("Getting DataFile {0}", dataFile));

            string targetPath = Path.Combine(Utils.modPath, dataFile);      
            /* if it doesn't exist yet, write out the original into persistentDataPath */
            if (!File.Exists(targetPath))
            {
                string originalData = orig_GetDataFile(dataFile);
                File.WriteAllText(targetPath, originalData);
            }
            string result = File.ReadAllText(targetPath);
            return result;
        }
    }

}
