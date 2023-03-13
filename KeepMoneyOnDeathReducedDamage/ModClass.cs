using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HutongGames.PlayMaker.Actions;
using HutongGames.PlayMaker;
using Satchel;
using Modding;
using UnityEngine;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using System.ComponentModel;
using MonoMod.RuntimeDetour;
using UnityEngine.SceneManagement;

namespace HKBalancedDifficultyMod
{
    public class HKBalancedDifficultyMod : Mod
    {
        new public string GetName() => "Hollow Knight Balanced Difficulty";
        public override string GetVersion() => "0.1.3";

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

            if (GameManager.instance != null && GameManager.instance.gameMap != null && HeroController.instance != null && to.name != "Menu_Title" && to.name != "Quit_To_Menu")
            {
                GameManager.instance.AddToScenesVisited(to.name);

                Log("Player Map Update " + GameManager.instance.UpdateGameMap());
                GameManager.instance.gameMap.GetComponent<GameMap>().SetupMap(false);
            }
        }

        private bool PlayerData_GetBool(On.PlayerData.orig_GetBool orig, PlayerData self, string boolName)
        {
            //Charm 2 is compass
            if (boolName == "equippedCharm_2" || boolName == "hasQuill")
                return true;
            else return orig(self, boolName);
        }

        private void BossSceneController_ReportHealth(On.BossSceneController.orig_ReportHealth orig, HealthManager healthManager, int baseHP, int adjustedHP, bool forceAdd)
        {
            orig(healthManager, baseHP, baseHP, forceAdd);
        }

        private void HeroController_ClearMP(On.HeroController.orig_ClearMP orig, HeroController self)
        {
            //NOP
            //Only way I can find to not clear MP on respawn
        }

        private int ModHooks_AfterTakeDamageHook(int hazardType, int damageAmount)
        {
            if (damageAmount > 1)
                return damageAmount / 2;
            return damageAmount;
        }

        private void HeroController_Start(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

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
    }
}
