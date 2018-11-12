using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MonoMod;
using UnityEngine;
using Assembly_CSharp;
#pragma warning disable CS0626
namespace Assembly_CSharp
{
    public static class Utils
    {
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
                Tex2D = new Texture2D(40, 40, TextureFormat.RGBA32, false);           // Create new "empty" texture
                if (Tex2D.LoadImage(FileData))
                {           // Load the imagedata into the texture (size is set automatically)
                    Tex2D.filterMode = FilterMode.Point;
                    Tex2D.wrapMode = TextureWrapMode.Clamp;
                    Tex2D.anisoLevel = 1;
                    return Tex2D;                 // If data = readable -> return texture
                }
            }
            return null;                     // Return null if load failed
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
            foreach (string path in GetModFiles(".png"))
            {
                Debug.Log(String.Format("Okay, I'm going to try and load {0}", path));
                Sprite new_sprite = Utils.LoadNewSprite(path);
                this.sprites[Path.GetFileNameWithoutExtension(path)] = new_sprite;
            }
        }

        public List<string> GetModFiles(string postfix)
        {
            Debug.Log(String.Format("We're looking for files ending with {0}", postfix));
            string modDirectory = Path.Combine(Application.persistentDataPath, "Mods");
            /* Make the mod directory if it doesn't exist. */
            DirectoryInfo di = Directory.CreateDirectory(modDirectory); // Don't need to check first.
            string[] filenames = Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories);
            List<string> finalList = new List<string>();
            foreach (string path in filenames)
            {
                if (path.EndsWith(postfix, StringComparison.CurrentCultureIgnoreCase))
                {
                    Debug.Log(String.Format("Loading additional data from {0}", path));
                    finalList.Add(path);
                }
            }
            return finalList;

        }

        /* patch CreateSpellObjectPrototypes to read mod files */
        public extern void orig_CreateSpellObjectPrototypes();
        public void CreateSpellObjectPrototypes()
        {
            orig_CreateSpellObjectPrototypes();

            foreach (string mod in GetModFiles("Spells.xml"))
            {
                this.ReadSpellFile(S.I.xmlReader.GetDataFile(mod));
            }
        }

        /* broken out from original CreateArtifactObjectPrototypes */
        public void ReadArtifactFile(string xmlData)
        {
            if (this.artDictionary == null)
            {
                this.artDictionary = new Dictionary<string, ArtifactObject>();
            }
            XmlTextReader xmlTextReader = new XmlTextReader(new StringReader(xmlData));
            int num = 0;
            if (xmlTextReader.ReadToDescendant("Artifacts") && xmlTextReader.ReadToDescendant("Artifact"))
            {
                do
                {
                    num++;
                    ArtifactObject artifactObject = new ArtifactObject();
                    artifactObject.ReadXmlPrototype(xmlTextReader);
                    artifactObject.sprite = this.GetSprite(artifactObject.itemID);
                    artifactObject.type = ItemType.Art;
                    artifactObject.artObj = artifactObject;
                    if (artifactObject.tags.Contains(Tag.BonusRe))
                    {
                        ArtifactObject artifactObject2 = artifactObject;
                        artifactObject2.flavor += " (BonusRe)";
                    }
                    this.artDictionary[artifactObject.itemID] = artifactObject;
                    this.itemDictionary[artifactObject.itemID] = artifactObject;
                }
                while (xmlTextReader.ReadToNextSibling("Artifact"));
            }
        }

        /* patch CreateArtifactObjectPrototypes */
        public extern void orig_CreateArtifactObjectPrototypes();
        public void CreateArtifactObjectPrototypes()
        {
            ReadArtifactFile(S.I.xmlReader.GetDataFile("Artifacts.xml"));
            foreach (string mod in GetModFiles("Artifacts.xml"))
            {
                ReadArtifactFile(S.I.xmlReader.GetDataFile(mod));
            }
        }

        /* patch LoadEffectsLua */
        public extern void orig_LoadEffectsLua();
        public void LoadEffectsLua()
        {
            string text = S.I.xmlReader.GetDataFile("SpellsL.txt");
            text = text.Insert(0, string.Format("{0} ", S.I.xmlReader.GetDataFile("LibL.txt")));
            text = text.Insert(0, string.Format("{0} ", S.I.xmlReader.GetDataFile("EffectsL.txt")));
            text = text.Insert(0, string.Format("{0} ", S.I.xmlReader.GetDataFile("ArtifactsL.txt")));
            foreach (string mod in GetModFiles(".lua"))
            {
                text = text.Insert(0, string.Format("{0} ", S.I.xmlReader.GetDataFile(mod)));
            }
            new EffectActions(text);
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
        [MonoModAdded]
        public Dictionary<string, string> cache;

        [MonoModConstructor]
        public void Constructor()
        { 
            cache = new Dictionary<string, string>();
        }

        public extern string orig_GetDataFile(string dataFile);
        public string GetDataFile(string dataFile)
        {
            Debug.Log(String.Format("Getting DataFile {0}", dataFile));
            /* Check if it exists in the cache already. */
            string cachedValue = "";
            if (cache.TryGetValue(dataFile, out cachedValue))
            {
                return cachedValue;
            }

            string targetPath = Path.Combine(Application.persistentDataPath, dataFile);
            /* if it doesn't exist yet, write out the original into persistentDataPath */
            if (!File.Exists(targetPath))
            {
                string originalData = orig_GetDataFile(dataFile);
                File.WriteAllText(targetPath, originalData);
            }

            string result = File.ReadAllText(targetPath);

            // Add it into the cache for next time.
            cache.Add(dataFile, result);
            return result;
        }
    }
}
