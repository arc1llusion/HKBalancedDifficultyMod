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

namespace HKBalancedDifficultyMod
{
    //public static class HealthManager
    //{
    //    public static class HPScaleGG
    //    {
    //        [EditorBrowsable(EditorBrowsableState.Never)]
    //        public delegate int orig_GetScaledHP(int originalHP);

    //        [EditorBrowsable(EditorBrowsableState.Never)]
    //        public delegate int hook_GetScaledHP(orig_GetScaledHP orig, int originalHP);

    //        public static event hook_GetScaledHP GetScaledHP
    //        {
    //            add
    //            {
    //                HookEndpointManager.Add<hook_GetScaledHP>(MethodBase.GetMethodFromHandle(RuntimeMethodHandle), value);
    //            }
    //            remove
    //            {
    //                HookEndpointManager.Remove<hook_GetScaledHP>(MethodBase.GetMethodFromHandle(RuntimeMethodHandle), value);
    //            }
    //        }
    //    }
    //}


    //public class ExcludePropertyResolver : DefaultContractResolver
    //{

    //    private readonly string[] _properties = new string[0];

    //    public ExcludePropertyResolver(params string[] properties)
    //    {
    //        _properties = new string[properties.Length];
    //        Array.Copy(properties, _properties, properties.Length);
    //    }

    //    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    //    {
    //        var currentProperties = base.CreateProperties(type, memberSerialization);

    //        currentProperties = currentProperties.Where(p => !this._properties.Contains(p.PropertyName)).ToList();

    //        return currentProperties;
    //    }
    //}

    public class HKBalancedDifficultyMod : Mod
    {
        new public string GetName() => "Hollow Knight Balanced Difficulty";
        public override string GetVersion() => "0.1.2";

        //private static MethodInfo origGetScaledHP = typeof(HealthManager).GetNestedType("HPScaleGG", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("GetScaledHP", BindingFlags.Public | BindingFlags.Instance);
        //private Hook OnGetScaledHP;

        //private string[] removeActionPropertiesFromSerialization =
        //    new string[]
        //    {
        //        "owner",
        //        "Owner",
        //        "gameObject",
        //        "GameObject",
        //        "rootFsm",
        //        "RootFsm",
        //        "fsm",
        //        "Fsm",
        //        "Position",
        //        "Rotation",
        //        "position",
        //        "rotation",
        //        "normalized",
        //        "State",
        //        "state",
        //        "Actions",
        //        "actions",
        //        "linear",
        //        "Linear",
        //        "gamma",
        //        "Gamma"
        //    };

        public override void Initialize()
        {
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.AfterTakeDamageHook += ModHooks_AfterTakeDamageHook;
            On.HeroController.Start += HeroController_Start;
            On.HeroController.ClearMP += HeroController_ClearMP;

            //On.HealthManager.HPScaleGG.GetScaledHP += HPScaleGG_GetScaledHP;
            //OnGetScaledHP = new Hook(origGetScaledHP, CustomGetScaledHP);
        }

        //private int HPScaleGG_GetScaledHP(On.HealthManager.HPScaleGG.orig_GetScaledHP orig, ref ValueType self, int originalHP)
        //{
        //    return originalHP;
        //}

        //private int CustomGetScaledHP(ValueType self, int originalHP) 
        //{            
        //    return originalHP;
        //}

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
            //    Log(string.Empty);
            //    LogGameObjectComponents(HeroController.instance.heroDeathPrefab);
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
                    //for (var i = 0; i < state.Actions.Length; ++i)
                    //{
                    //    //var act = state.Actions[i];

                    //    //if (act is SendEventByName)
                    //    //{
                    //    //    Log("----Event Send " + (act as SendEventByName).sendEvent.Value);
                    //    //}
                    //    //else if (act is SendEventToRegister)
                    //    //{
                    //    //    Log("----Event Register Send " + (act as SendEventToRegister).eventName.Value);
                    //    //}

                    //    var actionJson = JsonConvert.SerializeObject(state.Actions[i], Formatting.Indented, new JsonSerializerSettings
                    //    {
                    //        ContractResolver = new ExcludePropertyResolver(removeActionPropertiesFromSerialization)
                    //    });

                    //    Log("---- " + actionJson);
                    //}
                }

                //Log("-Transitions");
                //foreach (var t in fsm.FsmGlobalTransitions)
                //{
                //    Log("--Transition Name" + t.EventName);
                //}
            }
        }
    }
}
