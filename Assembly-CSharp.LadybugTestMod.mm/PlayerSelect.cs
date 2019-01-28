using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using UnityEngine;

namespace Assembly_CSharp
{
    class PlayerSelect
    {
        private static List<string> moddedChars = new List<string>();

        public static string[] getCharacters() {
            string[] chars = new string[3 + moddedChars.Count];
            chars[0] = "SaffronDemo";
            chars[1] = "SaffronDemoCiel";
            chars[2] = "RevaDemo";
            int i = 3;
            foreach (string s in moddedChars)
                chars[i++] = s;
            return chars;
        }

        [MonoModPatch("global::OptionCtrl")]
        public class patch_RevaDisplay : OptionCtrl
        {
            //Store selection information through reloads
            private static int option = 0;
            public static string heroID = "SaffronDemo";
            public static string heroAnim = "Saffron";
            public static string heroName = "Saffron";
            //Used to track when skins should be reloaded
            private static bool setup = false;

            [MonoModPublic]
            private ItemManager itemMan;
            [MonoModPublic]
            private BC ctrl;

            [MonoModReplace]
            public void ClickEnableReva()
            {
                option++;
                if (option > PlayerSelect.getCharacters().Length - 1)
                {
                    option = 0;
                }
                this.UpdateRevaDisplay();
            }
            //Disable the Skin Picker
            [MonoModReplace]
            private void UpdateOutfitDisplay()
            {
                outfitDisplay.enabled = false;
                outfitText.text = "";
                outfitDisplay.gameObject.SetActive(false);
            }

            [MonoModReplace]
            public void UpdateRevaDisplay()
            {
                revaDisplay.updateMode = AnimatorUpdateMode.UnscaledTime;
                //NPE Prevention
                if (ctrl.runCtrl.spCtrl == null)
                {
                    PlayerPrefs.SetInt("RevaDemoEnabled", 0);
                    revaText.text = heroName;
                    revaDisplay.runtimeAnimatorController = this.itemMan.GetAnim(heroAnim);
                    setup = false;
                    return;
                }
                if (!setup) {
                    setupCustomsAndSkins();
                    setup = true;
                }
                BeingObject hero = ctrl.runCtrl.spCtrl.heroDictionary[PlayerSelect.getCharacters()[option]];
                revaDisplay.runtimeAnimatorController = this.itemMan.GetAnim(hero.animName);
                revaDisplay.SetTrigger("spawn");
                revaText.text = hero.nameString;
                heroID = hero.beingID;
                heroAnim = hero.animName;
                heroName = hero.nameString;
            }

            private void setupCustomsAndSkins(){
                //Skins handled here (Just Ciel for now)
                Dictionary<string,BeingObject> heroDictionary = ctrl.runCtrl.spCtrl.heroDictionary;
                BeingObject SaffronCiel = heroDictionary["SaffronDemo"].Clone();
                SaffronCiel.animName = "SaffronCiel";
                heroDictionary.Add("SaffronDemoCiel", SaffronCiel);
                ctrl.runCtrl.spCtrl.heroDictionary = heroDictionary;
                
                //Find characters to add to the menu
                moddedChars.Clear();
                foreach (KeyValuePair<String, BeingObject> entry in heroDictionary)
                    if (entry.Value.tags.Contains("Selectable"))
                        moddedChars.Add(entry.Value.beingID);
            }
        }

        //Actually set the player to the selection
        [MonoModPatch("global::RunCtrl")]
        public class patch_RunCtrl : RunCtrl {
            public extern void orig_ResetWorld();
            public void ResetWorld(){
                orig_ResetWorld();
                defaultHeroString = patch_RevaDisplay.heroID;
                demoHeroString = patch_RevaDisplay.heroID;
            }
        }
        //Make players spawn based on the new values
        [MonoModPatch("global::SpawnCtrl")]
        public class patch_SpawnCtrl : SpawnCtrl
        {
            [MonoModReplace]
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
}
