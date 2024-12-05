using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Assertions;
using System.Diagnostics;
using DG.Tweening;

namespace MoongateInvasion;

[HarmonyPatch]
class MoongateInvasionHack
{
    // Zone_User.IsUserZone get
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone_User), "get_IsUserZone")]
    static bool IsUserZonePrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    // Zone_User.HasLaw get
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone_User), "get_HasLaw")]
    static bool HasLawPrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    // Zone_User.IsUserZone get
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone_User), "get_RevealRoom")]
    static bool RevealRoomPrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    // ZOne_User.MakeTownProperties get
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone_User), "get_MakeTownProperties")]
    static bool MakeTownPropertiesPrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    // Zone.UseFog get
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone), "get_UseFog")]
    static bool UseFogPrefix(Zone __instance, ref bool __result)
    {
        if (__instance is Zone_User)
        {
            __result = true;
            return false;
        }
        return true;
    }
    // Zone.Activate
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Zone), "Activate")]
    static void ActivatePrefix(Zone __instance)
    {
        if (__instance is Zone_User)
        {
            __instance.events.Add(new ZoneEventMoongateInvasion());
        }
    }

/*
    // Zone.Activate
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Zone), "Activate")]
    static void ActivatePostfix(Zone __instance)
    {
        if (__instance is Zone_User)
        {
            // MoongateInvasion.ModifyUserMap(__instance);
        }
    }
*/

    // TraitMagicChest.CanOpenContainer
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TraitMagicChest), "get_CanOpenContainer")]
    static void CanOpenContainerPostfix(ref bool __result)
    {
        if (EClass._zone is Zone_User)
        {
            __result = true;
        }
    }
/*
    // This could be risky...
    static bool isStored = false;
    static bool storedFlag = false;
    // Scene.OnUpdate
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Scene), "OnUpdate")]
    static void OnUpdatePrefix(Scene __instance)
    {
        if (EClass.core != null && EClass.core.game != null && EClass._zone != null && EClass._zone is Zone_User)
        {
            Plugin.ModLog($"Storing flag {EClass.game.Difficulty.allowRevive}");
            storedFlag = EClass.game.Difficulty.allowRevive;
            isStored = true;
            EClass.game.Difficulty.allowRevive = false;
        }
    }

    // Scene.OnUpdate
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Scene), "OnUpdate")]
    static void OnUpdatePostfix(Scene __instance)
    {
        if (EClass.core != null && EClass.core.game != null && EClass._zone != null && EClass._zone is Zone_User)
        {
            if (isStored)
            {
            Plugin.ModLog("OnUpdatePostfix flag");
                EClass.game.Difficulty.allowRevive = storedFlag;
                isStored = false;
            }
        }
    }*/
}