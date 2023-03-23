using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HKBalancedDifficultyMod
{
    public class HKBalancedDifficultyMod : Mod, IMenuMod, IGlobalSettings<GlobalSettings>
    {
        new public string GetName() => "Hollow Knight Balanced Difficulty";
        public override string GetVersion() => "0.1.8";

        private GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();

        public override void Initialize()
        {
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.AfterTakeDamageHook += ModHooks_AfterTakeDamageHook;
            On.HeroController.Start += HeroController_Start;
            On.HeroController.ClearMP += HeroController_ClearMP;
            On.BossSceneController.ReportHealth += BossSceneController_ReportHealth;
            On.PlayerData.GetBool += PlayerData_GetBool;
            On.GameMap.PositionCompass += GameMap_PositionCompass;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneManagerActiveSceneChanged;
        }

        private void GameMap_PositionCompass(On.GameMap.orig_PositionCompass orig, GameMap self, bool posShade)
        {
            orig(self, posShade);

            if (!GlobalSettings.PermanentCompass)
                return;

            self.compassIcon.SetActive(true);
            ReflectionHelper.SetFieldSafe(self, "displayingCompass", true);
        }

        private void OnSceneManagerActiveSceneChanged(Scene from, Scene to)
        {
            Log("Scene transitioned to " + to.name);
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

            if (boolName == nameof(PlayerData.instance.hasQuill))
                return true;
            else return orig(self, boolName);
        }

        private void BossSceneController_ReportHealth(On.BossSceneController.orig_ReportHealth orig, HealthManager healthManager, int baseHP, int adjustedHP, bool forceAdd)
        {
            if (BossSceneController.IsBossScene)
            {
                Log($"Adjusted HP Call {baseHP} {adjustedHP} {forceAdd}");
            }

            if (!GlobalSettings.PreventBossScaleHp)
            {
                orig(healthManager, baseHP, adjustedHP, forceAdd);
            }
            else
            {
                //Prevent scaled boss HP when nail upgrade in effect
                orig(healthManager, baseHP, baseHP, forceAdd);
            }
        }

        private void HeroController_ClearMP(On.HeroController.orig_ClearMP orig, HeroController self)
        {
            if (!GlobalSettings.PreventShade)
            {
                orig(self);
            }
            //else NOP
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

            ToggleGeoSoulLossAndShadeSpawnFromDeathSequence();
            ToggleSoulSpeedIncrease();
        }

        private void ToggleGeoSoulLossAndShadeSpawnFromDeathSequence()
        {
            if (HeroController.instance == null || HeroController.instance.heroDeathPrefab == null) return;

            var go = HeroController.instance.heroDeathPrefab;
            var fsm = go.LocateMyFSM("Hero Death Anim");
            var enabled = !GlobalSettings.PreventShade;

            ToggleActionsFromState(fsm, "Break Glass HP", enabled);
            ToggleActionsFromState(fsm, "Break Glass Geo", enabled);
            ToggleActionsFromState(fsm, "Break Glass Attack", enabled);

            ToggleActionsFromState(fsm, "Remove Overcharm", enabled);

            ToggleActionsFromState(fsm, "Remove Geo", enabled);
            ToggleActionsFromState(fsm, "Set Shade", enabled);

            ToggleActionsFromState(fsm, "Check MP", enabled);

            ToggleActionsFromState(fsm, "Notify Geo Counter", enabled);
            ToggleActionsFromState(fsm, "Drain Soul", enabled);
            ToggleActionsFromState(fsm, "Drain Soul 2", enabled);

            //ToggleActionsFromState(fsm, "Bursting", enabled);
            ToggleActionsFromState(fsm, "Break Msg", enabled);
            ToggleActionsFromState(fsm, "Shade?", enabled);
            ToggleActionsFromState(fsm, "No Shade", enabled);
            ToggleActionsFromState(fsm, "Limit Soul", enabled);
            ToggleActionsFromState(fsm, "Limit Soul?", enabled);

            fsm.GetState("End").Actions[0].Enabled = enabled; //Toggles the Start Soul Limiter Action
            fsm.GetState("End").Actions[1].Enabled = enabled; //Toggles the Soul Limiter UP Action
        }

        private void ToggleActionsFromState(PlayMakerFSM fsm, string stateName, bool enabled = false)
        {
            var state = fsm.GetState(stateName);

            var length = state.Actions.Length;
            for (var i = 0; i < length; ++i)
            {
                state.Actions[i].Enabled = enabled; 
            }
        }

        private void ToggleSoulSpeedIncrease()
        {
            if (HeroController.instance == null || HeroController.instance.spellControl == null) return;

            var spellControl = HeroController.instance.spellControl;
            var deepFocusState = spellControl.GetState("Deep Focus Speed");
            var timePerMpDrain = spellControl.FsmVariables.FindFsmFloat("Time Per MP Drain");
            var boolCharmTest = deepFocusState.Actions.FirstOrDefault(x => x is PlayerDataBoolTest);
            var originalFocusMultiply = deepFocusState.Actions.LastOrDefault(x => x is FloatMultiply);

            if (GlobalSettings.IncreaseFocusSpeed)
            {
                deepFocusState.Actions = new FsmStateAction[]
                {
                    new FloatMultiply
                    {
                        floatVariable = timePerMpDrain,
                        multiplyBy = 0.5f,
                        everyFrame = false
                    },
                    boolCharmTest,
                    originalFocusMultiply
                };
            }
            else
            {
                deepFocusState.Actions = new FsmStateAction[]
                {
                    boolCharmTest,
                    originalFocusMultiply
                };
            }
        }


        public void OnHeroUpdate()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                LogGameObjectComponents(HeroController.instance.spellControl.gameObject);


                //foreach (var s in GameObject.FindObjectsOfType<MonoBehaviour>())
                //{
                //    Log("Object name in scene: " + s.name);
                //}
                //Log(string.Empty);
                //LogGameObjectComponents(HeroController.instance.heroDeathPrefab);
            }
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
                    Log("--Event Name" + ev.Name + " " + ev.IsGlobal);
                }

                Log("-States");

                foreach (var state in fsm.FsmStates)
                {
                    Log("---State Name " + state.Name);

                    Log("----Transitions");

                    foreach (var transition in state.Transitions)
                    {
                        Log($"-----Transition: {transition.ToState}:{transition.EventName}:{transition.LinkConstraint}");
                    }
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
                        this.ToggleGeoSoulLossAndShadeSpawnFromDeathSequence();
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
                },
                new IMenuMod.MenuEntry
                {
                    Name = "Increase Focus Speed",
                    Description = "Increases focus speed by half.",
                    Values = new string[]
                    {
                        "Off",
                        "On"
                    },
                    Saver = (opt) =>
                    {
                        this.GlobalSettings.IncreaseFocusSpeed = SaveBoolSwitch(opt);
                        ToggleSoulSpeedIncrease();
                    },
                    Loader = () => LoadBoolSwitch(this.GlobalSettings.IncreaseFocusSpeed)
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
