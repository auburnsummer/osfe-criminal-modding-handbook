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

    [MonoModPatch("global::DiscordRpc")]
    public static class DiscordRpc
    {
        // Token: 0x0600077E RID: 1918
        [MonoModReplace]
        [DllImport("libdiscord-rpc.bundle", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Discord_Initialize")]
        public static extern void Initialize(string applicationId, ref DiscordRpc.EventHandlers handlers, bool autoRegister, string optionalSteamId);

        // Token: 0x0600077F RID: 1919
        [MonoModReplace]
        [DllImport("libdiscord-rpc.bundle", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Discord_Shutdown")]
        public static extern void Shutdown();

        // Token: 0x06000780 RID: 1920
        [MonoModReplace]
        [DllImport("libdiscord-rpc.bundle", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Discord_RunCallbacks")]
        public static extern void RunCallbacks();

        // Token: 0x06000781 RID: 1921
        [MonoModReplace]
        [DllImport("libdiscord-rpc.bundle", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Discord_UpdatePresence")]
        private static extern void UpdatePresenceNative(ref DiscordRpc.RichPresenceStruct presence);

        // Token: 0x06000782 RID: 1922
        [MonoModReplace]
        [DllImport("libdiscord-rpc.bundle", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Discord_ClearPresence")]
        public static extern void ClearPresence();

        // Token: 0x06000783 RID: 1923
        [MonoModReplace]
        [DllImport("libdiscord-rpc.bundle", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Discord_Respond")]
        public static extern void Respond(string userId, DiscordRpc.Reply reply);

        // Token: 0x06000784 RID: 1924
        [MonoModReplace]
        [DllImport("libdiscord-rpc.bundle", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Discord_UpdateHandlers")]
        public static extern void UpdateHandlers(ref DiscordRpc.EventHandlers handlers);

        // Token: 0x06000785 RID: 1925 RVA: 0x0002D610 File Offset: 0x0002BA10
        public static void UpdatePresence(DiscordRpc.RichPresence presence)
        {
            DiscordRpc.RichPresenceStruct @struct = presence.GetStruct();
            DiscordRpc.UpdatePresenceNative(ref @struct);
            presence.FreeMem();
        }

        // Token: 0x0400097A RID: 2426
        private const string LibName = "libdiscord-rpc";

        // Token: 0x0200011E RID: 286
        // (Invoke) Token: 0x06000787 RID: 1927
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ReadyCallback(ref DiscordRpc.DiscordUser connectedUser);

        // Token: 0x0200011F RID: 287
        // (Invoke) Token: 0x0600078B RID: 1931
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DisconnectedCallback(int errorCode, string message);

        // Token: 0x02000120 RID: 288
        // (Invoke) Token: 0x0600078F RID: 1935
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ErrorCallback(int errorCode, string message);

        // Token: 0x02000121 RID: 289
        // (Invoke) Token: 0x06000793 RID: 1939
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void JoinCallback(string secret);

        // Token: 0x02000122 RID: 290
        // (Invoke) Token: 0x06000797 RID: 1943
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SpectateCallback(string secret);

        // Token: 0x02000123 RID: 291
        // (Invoke) Token: 0x0600079B RID: 1947
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RequestCallback(ref DiscordRpc.DiscordUser request);

        // Token: 0x02000124 RID: 292
        public struct EventHandlers
        {
            // Token: 0x0400097B RID: 2427
            public DiscordRpc.ReadyCallback readyCallback;

            // Token: 0x0400097C RID: 2428
            public DiscordRpc.DisconnectedCallback disconnectedCallback;

            // Token: 0x0400097D RID: 2429
            public DiscordRpc.ErrorCallback errorCallback;

            // Token: 0x0400097E RID: 2430
            public DiscordRpc.JoinCallback joinCallback;

            // Token: 0x0400097F RID: 2431
            public DiscordRpc.SpectateCallback spectateCallback;

            // Token: 0x04000980 RID: 2432
            public DiscordRpc.RequestCallback requestCallback;
        }

        // Token: 0x02000125 RID: 293
        [Serializable]
        public struct RichPresenceStruct
        {
            // Token: 0x04000981 RID: 2433
            public IntPtr state;

            // Token: 0x04000982 RID: 2434
            public IntPtr details;

            // Token: 0x04000983 RID: 2435
            public long startTimestamp;

            // Token: 0x04000984 RID: 2436
            public long endTimestamp;

            // Token: 0x04000985 RID: 2437
            public IntPtr largeImageKey;

            // Token: 0x04000986 RID: 2438
            public IntPtr largeImageText;

            // Token: 0x04000987 RID: 2439
            public IntPtr smallImageKey;

            // Token: 0x04000988 RID: 2440
            public IntPtr smallImageText;

            // Token: 0x04000989 RID: 2441
            public IntPtr partyId;

            // Token: 0x0400098A RID: 2442
            public int partySize;

            // Token: 0x0400098B RID: 2443
            public int partyMax;

            // Token: 0x0400098C RID: 2444
            public IntPtr matchSecret;

            // Token: 0x0400098D RID: 2445
            public IntPtr joinSecret;

            // Token: 0x0400098E RID: 2446
            public IntPtr spectateSecret;

            // Token: 0x0400098F RID: 2447
            public bool instance;
        }

        // Token: 0x02000126 RID: 294
        [Serializable]
        public struct DiscordUser
        {
            // Token: 0x04000990 RID: 2448
            public string userId;

            // Token: 0x04000991 RID: 2449
            public string username;

            // Token: 0x04000992 RID: 2450
            public string discriminator;

            // Token: 0x04000993 RID: 2451
            public string avatar;
        }

        // Token: 0x02000127 RID: 295
        public enum Reply
        {
            // Token: 0x04000995 RID: 2453
            No,
            // Token: 0x04000996 RID: 2454
            Yes,
            // Token: 0x04000997 RID: 2455
            Ignore
        }

        // Token: 0x02000128 RID: 296
        public class RichPresence
        {
            // Token: 0x0600079F RID: 1951 RVA: 0x0002D648 File Offset: 0x0002BA48
            internal DiscordRpc.RichPresenceStruct GetStruct()
            {
                if (this._buffers.Count > 0)
                {
                    this.FreeMem();
                }
                this._presence.state = this.StrToPtr(this.state, 128);
                this._presence.details = this.StrToPtr(this.details, 128);
                this._presence.startTimestamp = this.startTimestamp;
                this._presence.endTimestamp = this.endTimestamp;
                this._presence.largeImageKey = this.StrToPtr(this.largeImageKey, 32);
                this._presence.largeImageText = this.StrToPtr(this.largeImageText, 128);
                this._presence.smallImageKey = this.StrToPtr(this.smallImageKey, 32);
                this._presence.smallImageText = this.StrToPtr(this.smallImageText, 128);
                this._presence.partyId = this.StrToPtr(this.partyId, 128);
                this._presence.partySize = this.partySize;
                this._presence.partyMax = this.partyMax;
                this._presence.matchSecret = this.StrToPtr(this.matchSecret, 128);
                this._presence.joinSecret = this.StrToPtr(this.joinSecret, 128);
                this._presence.spectateSecret = this.StrToPtr(this.spectateSecret, 128);
                this._presence.instance = this.instance;
                return this._presence;
            }

            // Token: 0x060007A0 RID: 1952 RVA: 0x0002D7DC File Offset: 0x0002BBDC
            private IntPtr StrToPtr(string input, int maxbytes)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return IntPtr.Zero;
                }
                string s = DiscordRpc.RichPresence.StrClampBytes(input, maxbytes);
                int byteCount = Encoding.UTF8.GetByteCount(s);
                IntPtr intPtr = Marshal.AllocHGlobal(byteCount);
                this._buffers.Add(intPtr);
                Marshal.Copy(Encoding.UTF8.GetBytes(s), 0, intPtr, byteCount);
                return intPtr;
            }

            // Token: 0x060007A1 RID: 1953 RVA: 0x0002D838 File Offset: 0x0002BC38
            private static string StrToUtf8NullTerm(string toconv)
            {
                string text = toconv.Trim();
                byte[] bytes = Encoding.Default.GetBytes(text);
                if (bytes.Length > 0 && bytes[bytes.Length - 1] != 0)
                {
                    text += "\0\0";
                }
                return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text));
            }

            // Token: 0x060007A2 RID: 1954 RVA: 0x0002D890 File Offset: 0x0002BC90
            private static string StrClampBytes(string toclamp, int maxbytes)
            {
                string text = DiscordRpc.RichPresence.StrToUtf8NullTerm(toclamp);
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                if (bytes.Length <= maxbytes)
                {
                    return text;
                }
                byte[] array = new byte[0];
                Array.Copy(bytes, 0, array, 0, maxbytes - 1);
                array[array.Length - 1] = 0;
                array[array.Length - 2] = 0;
                return Encoding.UTF8.GetString(array);
            }

            // Token: 0x060007A3 RID: 1955 RVA: 0x0002D8EC File Offset: 0x0002BCEC
            internal void FreeMem()
            {
                for (int i = this._buffers.Count - 1; i >= 0; i--)
                {
                    Marshal.FreeHGlobal(this._buffers[i]);
                    this._buffers.RemoveAt(i);
                }
            }

            // Token: 0x04000998 RID: 2456
            private DiscordRpc.RichPresenceStruct _presence;

            // Token: 0x04000999 RID: 2457
            private readonly List<IntPtr> _buffers = new List<IntPtr>(10);

            // Token: 0x0400099A RID: 2458
            public string state;

            // Token: 0x0400099B RID: 2459
            public string details;

            // Token: 0x0400099C RID: 2460
            public long startTimestamp;

            // Token: 0x0400099D RID: 2461
            public long endTimestamp;

            // Token: 0x0400099E RID: 2462
            public string largeImageKey;

            // Token: 0x0400099F RID: 2463
            public string largeImageText;

            // Token: 0x040009A0 RID: 2464
            public string smallImageKey;

            // Token: 0x040009A1 RID: 2465
            public string smallImageText;

            // Token: 0x040009A2 RID: 2466
            public string partyId;

            // Token: 0x040009A3 RID: 2467
            public int partySize;

            // Token: 0x040009A4 RID: 2468
            public int partyMax;

            // Token: 0x040009A5 RID: 2469
            public string matchSecret;

            // Token: 0x040009A6 RID: 2470
            public string joinSecret;

            // Token: 0x040009A7 RID: 2471
            public string spectateSecret;

            // Token: 0x040009A8 RID: 2472
            public bool instance;
        }
    }

}
