using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MonoMod;
using UnityEngine;
using Assembly_CSharp;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
#pragma warning disable CS0626
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

        public static SpriteAnimationClip LoadNewSpriteAnimationClip(string[] FilePaths, float PixelsPerUnit=100.0f, float KeyFrameLength=0.02f)
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

        public static void SpriteAnimationClipDebug(SpriteAnimationClip d)
        {
            Debug.Log("SAC DEBUG LOG");
        }
    }

    public class AnimClipTemplate : IComparable
    {
        public string fullPath { get; set; }
        public string key { get; set; }
        public int number { get; set; }

        public AnimClipTemplate(string fullPath, string key, int number)
        {
            this.fullPath = fullPath;
            this.key = key;
            this.number = number;
        }

        int IComparable.CompareTo(object obj)
        {
            AnimClipTemplate other = (AnimClipTemplate)obj;
            return number.CompareTo(other.number);
        }
    }

    [MonoModPatch("global::ItemManager")]
    public class patch_ItemManager : ItemManager
    {
        [MonoModPublic]
        [MonoModIgnore]
        public extern void ReadSpellFile(string xmlString);

        public void LoadCustomSpellIcons()
        {
            foreach (string path in Utils.GetModFiles("SpellIcons", ".png"))
            {
                Debug.Log(String.Format("Okay, I'm going to try and load {0} as a spell icon", path));
                Sprite new_sprite = Utils.LoadNewSprite(path);
                this.sprites[Path.GetFileNameWithoutExtension(path)] = new_sprite;
            }
        }

        public void LoadCustomAnimClips()
        {
            Debug.Log("LOADING THE ANIM CLIPS!!!!!");
            List<string> paths = Utils.GetModFiles("SpriteAnimationClips", ".png");


            Regex animationMatcher = new Regex(@"(\w+)_([0-9]+)", RegexOptions.IgnoreCase);
            Dictionary<string, List<AnimClipTemplate>> animClips = new Dictionary<string, List<AnimClipTemplate>>();
            foreach (string path in paths)
            {
                Match result = animationMatcher.Match(path);
                if (result.Success)
                {
                    AnimClipTemplate newAnimClipTemplate = new AnimClipTemplate(path, result.Groups[1].Value, int.Parse(result.Groups[2].Value));
                    if (!animClips.ContainsKey(newAnimClipTemplate.key))
                    {
                        animClips[newAnimClipTemplate.key] = new List<AnimClipTemplate>();
                    }
                    animClips[newAnimClipTemplate.key].Add(newAnimClipTemplate);
                }
            }

            foreach (string key in animClips.Keys)
            {
                Debug.Log(String.Format("Getting files for anim cli {0} ", key));
                List<AnimClipTemplate> currentClipList = animClips[key];
                currentClipList.Sort();
                // Make an anim clip now.
                List<string> fullPaths = new List<string>();
                foreach (AnimClipTemplate clip in currentClipList)
                {
                    fullPaths.Add(clip.fullPath);
                }
                SpriteAnimationClip newClip = Utils.LoadNewSpriteAnimationClip(fullPaths.ToArray());
                this.spriteAnimClips[key] = newClip;
                Debug.Log("Done with this anim clip!");
            }
        }

        public extern void orig_Awake();
        public void Awake()
        {
            Debug.Log("Hi from MonoMod!!");
            // UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
            orig_Awake();
            /* Load additional sprites. */
            LoadCustomSpellIcons();
            LoadCustomAnimClips();
        }

        public extern SpriteAnimationClip orig_GetClip(string clipName);
        public new SpriteAnimationClip GetClip(string clipName)
        {
            Debug.Log(String.Format("Getting SpriteAnimationClip {0}", clipName));
            SpriteAnimationClip theClip = orig_GetClip(clipName);
            return orig_GetClip(clipName);
        }
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
