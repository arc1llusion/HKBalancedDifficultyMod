using Modding;
using Satchel;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HKBalancedDifficultyMod
{
    public class HKBalancedDifficultyMod : Mod, IMenuMod, IGlobalSettings<GlobalSettings>
    {
        new public string GetName() => "Hollow Knight Balanced Difficulty";
        public override string GetVersion() => "0.1.5";

        private GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();

        public override void Initialize()
        {
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.AfterTakeDamageHook += ModHooks_AfterTakeDamageHook;
            On.HeroController.Start += HeroController_Start;
            On.HeroController.ClearMP += HeroController_ClearMP;
            On.BossSceneController.ReportHealth += BossSceneController_ReportHealth;
            On.PlayerData.GetBool += PlayerData_GetBool;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneManagerActiveSceneChanged;
        }

        private void OnSceneManagerActiveSceneChanged(Scene from, Scene to)
        {
            Log("Scene transitioed to " + to.name);
            if (!GlobalSettings.AutoUpdateMapOnSceneLoad) return;

            if (GameManager.instance != null && GameManager.instance.gameMap != null && HeroController.instance != null && to.name != "Menu_Title" && to.name != "Quit_To_Menu")
            {
                GameManager.instance.AddToScenesVisited(to.name);

                Log("Player Map Update " + GameManager.instance.UpdateGameMap());
                GameManager.instance.gameMap.GetComponent<GameMap>().SetupMap(false);
            }
        }

        private bool PlayerData_GetBool(On.PlayerData.orig_GetBool orig, PlayerData self, string boolName)
        {
            if (!GlobalSettings.PermanentCompass)
                return orig(self, boolName);

            //Charm 2 is compass
            if (boolName == "equippedCharm_2" || boolName == "hasQuill")
                return true;
            else return orig(self, boolName);
        }

        private void BossSceneController_ReportHealth(On.BossSceneController.orig_ReportHealth orig, HealthManager healthManager, int baseHP, int adjustedHP, bool forceAdd)
        {
            if (!GlobalSettings.PreventBossScaleHp)
            {
                orig(healthManager, baseHP, adjustedHP, forceAdd);
                return;
            };

            orig(healthManager, baseHP, baseHP, forceAdd);
        }

        private void HeroController_ClearMP(On.HeroController.orig_ClearMP orig, HeroController self)
        {
            //NOP
            //Only way I can find to not clear MP on respawn
        }

        private int ModHooks_AfterTakeDamageHook(int hazardType, int damageAmount)
        {
            if (!GlobalSettings.ReduceDamage) return damageAmount;

            if (damageAmount > 1)
                return damageAmount / 2;
            return damageAmount;
        }

        private void HeroController_Start(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

            if (!GlobalSettings.PreventShade) return;

            RemoveBullshitPartOfDeathSequence();
        }

        private void RemoveBullshitPartOfDeathSequence()
        {
            var go = HeroController.instance.heroDeathPrefab;
            var fsm = go.LocateMyFSM("Hero Death Anim");

            RemoveActionsFromState(fsm, "Break Glass HP");
            RemoveActionsFromState(fsm, "Break Glass Geo");
            RemoveActionsFromState(fsm, "Break Glass Attack");

            RemoveActionsFromState(fsm, "Remove Overcharm");

            RemoveActionsFromState(fsm, "Remove Geo");
            RemoveActionsFromState(fsm, "Set Shade");

            RemoveActionsFromState(fsm, "Check MP");

            RemoveActionsFromState(fsm, "Notify Geo Counter");
            RemoveActionsFromState(fsm, "Drain Soul");
            RemoveActionsFromState(fsm, "Drain Soul 2");

            RemoveActionsFromState(fsm, "Bursting");
            RemoveActionsFromState(fsm, "Break Msg");
            RemoveActionsFromState(fsm, "Shade?");
            RemoveActionsFromState(fsm, "No Shade");
            RemoveActionsFromState(fsm, "Limit Soul");
            RemoveActionsFromState(fsm, "Limit Soul?");

            fsm.GetState("End").RemoveAction(0);//Removes the Start Soul Limiter Action
            fsm.GetState("End").RemoveAction(0);//Removes the Soul Limiter UP Action
        }

        private void RemoveActionsFromState(PlayMakerFSM fsm, string stateName)
        {
            var state = fsm.GetState(stateName);

            Log($"{state.Name} {state.Actions.Length}");

            var length = state.Actions.Length;
            for (var i = 0; i < length; ++i)
            {
                state.RemoveAction(0); //Removing an action updates the array. If we want to remove all actions, just remove the first item each time
            }

            Log($"{state.Name} {state.Actions.Length}");
        }

        public void OnHeroUpdate()
        {
            //if (Input.GetKeyDown(KeyCode.O))
            //{
            //    LogGameObjectComponents(HeroController.instance.gameObject);


            //    foreach(var s in GameObject.FindObjectsOfType<MonoBehaviour>())
            //    {
            //        Log("Object name in scene: " + s.name);
            //    }
            //    //Log(string.Empty);
            //    //LogGameObjectComponents(HeroController.instance.heroDeathPrefab);
            //}
        }

        private void LogGameObjectComponents(GameObject go)
        {
            var fsms = go.GetComponents<PlayMakerFSM>();
            foreach (var fsm in fsms)
            {
                Log("FSM Name " + fsm.FsmName);

                Log("-Events");

                foreach (var ev in fsm.FsmEvents)
                {
                    Log("--Event Name" + ev.Name);
                }

                Log("-States");

                foreach (var state in fsm.FsmStates)
                {
                    Log("---State Name " + state.Name);
                }
            }
        }

        public bool ToggleButtonInsideMenu => false;
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            return new List<IMenuMod.MenuEntry>
            {
                new IMenuMod.MenuEntry
                {
                    Name = "Prevent Boss Scaled HP",
                    Description = "Prevent boss HP scaling on nail upgrade.",
                    Values = new string[]
                    {
                        "Off",
                        "On"
                    },
                    Saver = (opt) =>
                    {
                        this.GlobalSettings.PreventBossScaleHp = SaveBoolSwitch(opt);
                    },
                    Loader = () => LoadBoolSwitch(this.GlobalSettings.PreventBossScaleHp)
                },
                new IMenuMod.MenuEntry
                {
                    Name = "Auto Update Map On Scene Load",
                    Description = "Updates the Map when going to a new room instead of finding a bench.",
                    Values = new string[]
                    {
                        "Off",
                        "On"
                    },
                    Saver = (opt) =>
                    {
                        this.GlobalSettings.AutoUpdateMapOnSceneLoad = SaveBoolSwitch(opt);
                    },
                    Loader = () => LoadBoolSwitch(this.GlobalSettings.AutoUpdateMapOnSceneLoad)
                },
                new IMenuMod.MenuEntry
                {
                    Name = "Reduce Damage",
                    Description = "Reduces damage > 1 by half.",
                    Values = new string[]
                    {
                        "Off",
                        "On"
                    },
                    Saver = (opt) =>
                    {
                        this.GlobalSettings.ReduceDamage = SaveBoolSwitch(opt);
                    },
                    Loader = () => LoadBoolSwitch(this.GlobalSettings.ReduceDamage)
                },
                new IMenuMod.MenuEntry
                {
                    Name = "Prevent Shade",
                    Description = "Do not lose geo and soul on death.",
                    Values = new string[]
                    {
                        "Off",
                        "On"
                    },
                    Saver = (opt) =>
                    {
                        this.GlobalSettings.PreventShade = SaveBoolSwitch(opt);
                    },
                    Loader = () => LoadBoolSwitch(this.GlobalSettings.PreventShade)
                },
                new IMenuMod.MenuEntry
                {
                    Name = "Permanent Compass",
                    Description = "Makes it so the compass is permanently equipped regardless of charm.",
                    Values = new string[]
                    {
                        "Off",
                        "On"
                    },
                    Saver = (opt) =>
                    {
                        this.GlobalSettings.PermanentCompass = SaveBoolSwitch(opt);
                    },
                    Loader = () => LoadBoolSwitch(this.GlobalSettings.PermanentCompass)
                }
            };
        }

        private bool SaveBoolSwitch(int opt)
        {
            return opt switch
            {
                0 => false,
                1 => true,
                _ => throw new InvalidOperationException()
            };
        }

        private int LoadBoolSwitch(bool val)
        {
            return val switch
            {
                false => 0,
                true => 1
            };
        }

        public void OnLoadGlobal(GlobalSettings s)
        {
            GlobalSettings = s ?? new GlobalSettings();
        }

        public GlobalSettings OnSaveGlobal()
        {
            return GlobalSettings;
        }
    }
}
