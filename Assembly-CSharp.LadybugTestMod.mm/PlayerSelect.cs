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
        public static string[] getCharacters() {
            return new string[] {"SaffronDemo","SaffronDemoCiel", "RevaDemo"};
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
                    revaText.text = heroName;
                    revaDisplay.runtimeAnimatorController = this.itemMan.GetAnim(heroAnim);
                    setup = false;
                    return;
                }
                Dictionary<string, BeingObject> heroDictionary = ctrl.runCtrl.spCtrl.heroDictionary;
                //Handle skins(for now, just SaffronCiel)
                if (!setup) {
                    setupSkins(heroDictionary);
                    setup = true;
                }
                BeingObject hero = heroDictionary[PlayerSelect.getCharacters()[option]];
                revaDisplay.runtimeAnimatorController = this.itemMan.GetAnim(hero.animName);
                revaDisplay.SetTrigger("spawn");
                revaText.text = hero.nameString;
                heroID = hero.beingID;
                heroAnim = hero.animName;
                heroName = hero.nameString;
            }

            private void setupSkins(Dictionary<string, BeingObject> heroDictionary)
            {
                BeingObject orig_Saffron = heroDictionary["SaffronDemo"];
                BeingObject SaffronCiel = orig_Saffron.Clone();
                SaffronCiel.animName = "SaffronCiel";
                heroDictionary.Add("SaffronDemoCiel", SaffronCiel);
            }
        }
        //Make players spawn based on the new values
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
}
