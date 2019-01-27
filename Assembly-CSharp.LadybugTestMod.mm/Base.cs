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
    public class AnimClipFrame : IComparable
    {
        public string fullPath { get; set; }
        public string key { get; set; }
        public int number { get; set; }

        public AnimClipFrame(string fullPath, string key, int number)
        {
            this.fullPath = fullPath;
            this.key = key;
            this.number = number;
        }

        int IComparable.CompareTo(object obj)
        {
            AnimClipFrame other = (AnimClipFrame)obj;
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

            // Pattern to be considered a frame: alphanumeric "ID" portion and the frame number, seperated with an underscore.
            Regex animationMatcher = new Regex(@"(\w+)_([0-9]+)", RegexOptions.IgnoreCase);
            Dictionary<string, List<AnimClipFrame>> animClips = new Dictionary<string, List<AnimClipFrame>>();
            foreach (string path in paths)
            {
                Match result = animationMatcher.Match(path);
                if (result.Success)
                {
                    AnimClipFrame newAnimClipFrame = new AnimClipFrame(path, result.Groups[1].Value, int.Parse(result.Groups[2].Value));
                    // Make a new list for the anim clip key if it doesn't exist already
                    if (!animClips.ContainsKey(newAnimClipFrame.key))
                    {
                        animClips[newAnimClipFrame.key] = new List<AnimClipFrame>();
                    }
                    animClips[newAnimClipFrame.key].Add(newAnimClipFrame);
                }
            }

            foreach (string key in animClips.Keys)
            {
                Debug.Log(String.Format("Getting files for anim clip {0} ", key));
                List<AnimClipFrame> currentFrames = animClips[key];
                // sorting is required since the files could be like
                // Sprite_9.png, Sprite_10.png (the 10 would be first in lexographic sort!)
                currentFrames.Sort();

                // Make an anim clip now.
                List<string> fullPaths = new List<string>();
                foreach (AnimClipFrame clip in currentFrames)
                {
                    fullPaths.Add(clip.fullPath);
                }
                // TODO: get keyFrameLength from an XML file, maybe PixelsPerUnit too?
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

    //machinespray messing around with a character selector
    [MonoModPatch("global::OptionCtrl")]
    public class patch_RevaDisplay : OptionCtrl
    {
        //Store selection information through reloads
        private static int option = 0;
        public static string heroID = "SaffronDemo";
        public static string heroAnim = "Saffron";
        public static string heroName = "Saffron";
        [MonoModPublic]
        private ItemManager itemMan;
        [MonoModPublic]
        private BC ctrl;

        [MonoModReplace]
        public void ClickEnableReva()
        {
            option++;
            if (option > Utils.getCharacters().Length - 1)
            {
                option = 0;
            }
            this.UpdateRevaDisplay();
        }
        [MonoModReplace]
        private void UpdateOutfitDisplay()
        {
        }

        [MonoModReplace]
        public void UpdateRevaDisplay()
        {
            //NPE Prevention
            bool flag = ctrl.runCtrl.spCtrl == null;
            revaDisplay.updateMode = AnimatorUpdateMode.UnscaledTime;
            if (flag)
            {
                revaText.text = heroName;
                revaDisplay.runtimeAnimatorController = this.itemMan.GetAnim(heroAnim);
                return;
            }
            BeingObject hero = ctrl.runCtrl.spCtrl.heroDictionary[Utils.getCharacters()[option]];
            revaDisplay.runtimeAnimatorController = this.itemMan.GetAnim(hero.animName);
            revaText.text = hero.nameString;
            heroID = hero.beingID;
            heroAnim = hero.animName;
            heroName = hero.nameString;
        }
    }
    [MonoModPatch("global::SpawnCtrl")]
    public class patch_SpawnCtrl : SpawnCtrl
    {
        public void CreatePlayers()
        {
            if (S.I.AUTOMATION)
            {
                Player component = this.PlaceBeing(patch_RevaDisplay.heroID, TI.tileGrid[this.playerSpawnPos[0].x, this.playerSpawnPos[0].y], 0, false).GetComponent<Player>();
                component.gameObject.GetComponent<Animator>().runtimeAnimatorController = this.GetAnim(patch_RevaDisplay.heroAnim);
                component.StartCoroutine(component.TestLoop());
            }
            else
            {
                Being hero = PlaceBeing(patch_RevaDisplay.heroID, TI.tileGrid[this.playerSpawnPos[0].x, this.playerSpawnPos[0].y], 0, false);
                hero.gameObject.GetComponent<Animator>().runtimeAnimatorController = this.GetAnim(patch_RevaDisplay.heroAnim);
            }
            if (S.I.FAIRY)
            {
                this.CreateFairy();
            }
        }

    }
}
