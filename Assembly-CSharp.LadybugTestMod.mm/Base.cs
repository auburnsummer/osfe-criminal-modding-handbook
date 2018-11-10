using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MonoMod;
using UnityEngine;
#pragma warning disable CS0626
namespace Modding
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
            string modDirectory = Path.Combine(Application.persistentDataPath, "Mods");
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


        public extern void orig_CreateSpellObjectPrototypes();
        public void CreateSpellObjectPrototypes()
        {
            orig_CreateSpellObjectPrototypes();

            foreach (string mod in GetModFiles("Spells.xml"))
            {
                this.ReadSpellFile(S.I.xmlReader.GetDataFile(mod));
            }
        }

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
                    this.artDictionary[artifactObject.itemID] = artifactObject;
                    this.itemDictionary[artifactObject.itemID] = artifactObject;
                }
                while (xmlTextReader.ReadToNextSibling("Artifact"));
            }
        }

        public extern void orig_CreateArtifactObjectPrototypes();
        public void CreateArtifactObjectPrototypes()
        {
            Debug.Log("ENTERING ARTIFACTS");
            ReadArtifactFile(S.I.xmlReader.GetDataFile("Artifacts.xml"));
            foreach (string mod in GetModFiles("Artifacts.xml"))
            {
                Debug.Log("REACHED HERE");
                ReadArtifactFile(S.I.xmlReader.GetDataFile(mod));
            }
        }

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
            Debug.Log(string.Format("Getting DataFile {0}", dataFile));
            /* Check if it exists in the cache already. */
            string cachedValue = "";
            if (cache.TryGetValue(dataFile, out cachedValue))
            {
                Debug.Log("Getting cached value for this one.");
                return cachedValue;
            }


            string targetPath = Path.Combine(Application.persistentDataPath, dataFile);
            Debug.Log(string.Format("TargetPath: {0}", targetPath));
            /* if it doesn't exist yet, write out the original persistentDataPath */
            if (!File.Exists(targetPath))
            {
                string originalData = orig_GetDataFile(dataFile);
                File.WriteAllText(targetPath, originalData);
            }

            string result = File.ReadAllText(targetPath);

            /* Now return the text with the mods added! */
            cache.Add(dataFile, result);
            Debug.Log("Getting full value for this one.");
            Debug.Log(result);
            return result;
        }
    }
}
