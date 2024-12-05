using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Assertions;
using System.Diagnostics;
using DG.Tweening;

namespace MoongateInvasion;

public enum ContainerPrivateType
{
    eq,
    eq_2,
    eq_3,
    food,
    food_raw,
    book,
    steal,
    black_market,
    furniture,
    defaultType,
    __NUM,
    troll
}


class ZoneEventMoongateInvasion : ZoneEventQuest
{
    List<Chara> userMapCharaList = new List<Chara>();
    List<int> userMapCharaLVList = new List<int>();
    List<Thing> userMapContainerList = new List<Thing>();
    int maxLV = 0;
    int currentEnemyNum
    {
        get
        {
            if (!isTotalSuccessTriggered && zone.map.charas.Count == pc.party.members.Count)
            {
                OnKillAll();
            }
            return zone.map.charas.Count - pc.party.members.Count;
        }
    }
    [JsonProperty]
    int maxEnemyNum = 0;
    
    [JsonProperty]
    bool Initialized = false;
    [JsonProperty]
    bool isTotalSuccessTriggered = false;

    static bool DEBUG_MODE = false;

    public ZoneEventMoongateInvasion()
    {
        userMapCharaList.Clear();
        userMapCharaLVList.Clear();
        userMapContainerList.Clear();
        userMapCharaLVList.Add(30);
        maxLV = 0;
    }
    
    public override string TextWidgetDate
	{
		get
		{
			return $" Enemies: {maxEnemyNum - currentEnemyNum} / {maxEnemyNum} ";
		}
	}
	public override int TimeLimit
	{
		get
		{
			return -1;
		}
	}
    public override void OnVisit()
	{
        Plugin.ModLog("OnVisit", PrivateLogLevel.Debug);
        ModifyUserMap(this.zone);
    }

	public override void OnLeaveZone()
	{
        Plugin.ModLog("OnLeaveZone", PrivateLogLevel.Debug);
        if (zone.isDeathLocation)
        {
            Plugin.ModLog("Invasion fail", PrivateLogLevel.Info);
            OnFailExit();
            return;
        }
        else
        {
            Plugin.ModLog("Invasion success", PrivateLogLevel.Info);
            OnSuccessExit();
            return;
        }
    }


    public Card CardAddTag(Card card)
    {
        // All taged Cards will be destroy when invasion fail
        card.SetBool(10086, true);
        return card;
    }

    public int GetContainerLVByIndex(int index)
    {
        if (userMapContainerList.Count < 2)
        {
            return 0;
        }
        if (userMapContainerList.Count < userMapCharaLVList.Count)
        {
            return userMapCharaLVList[index];
        }
        return userMapCharaLVList[index * userMapCharaLVList.Count / 4 / (userMapContainerList.Count - 1)];
    }

    public void ModifyChara(Zone __instance, Chara chara)
    {
        Plugin.ModLog($"ModifyChara: {chara.Name} {chara.LV}", PrivateLogLevel.Debug);
        userMapCharaList.Add(chara);
        userMapCharaLVList.Add(chara.LV);
        maxLV = Math.Max(maxLV, chara.LV);
        maxEnemyNum++;

        // Set hostility
        if (DEBUG_MODE)
        {
            chara.c_originalHostility = Hostility.Neutral;
            chara.hostility = Hostility.Neutral;
        }
        else
        {
            chara.c_originalHostility = Hostility.Enemy;
            chara.hostility = Hostility.Enemy;
        }

        // Replace it if it's too close to the player
        Point point = chara.pos;
        for (int i = 0; i < 100; i++)
        {
            if (point.Distance(EClass.pc.pos) < 16)
            {
                point = __instance.map.GetRandomPoint();
            }
            else
            {
                break;
            }
        }
        chara.Teleport(point, true, true);

        // Add inventory
        if (chara.LV > 20)
        {
            for (int i = 0; i < Math.Min(EClass.rndHalf(chara.LV), 8); i++)
            {
                chara.AddCard(CardAddTag(ThingGen.CreateFromFilter("eq", chara.LV)));
            }
        }
    }

    public void ModifyContainer(Thing container, int index)
    {
        int containerLV = GetContainerLVByIndex(index) + (int)Mathf.Sqrt(userMapCharaLVList.Count);
        int generatedNum = Math.Min(Math.Min(containerLV, EClass.rndHalf(container.things.GridSize)), EClass.rndHalf(20));
        ContainerPrivateType containerType = (ContainerPrivateType)EClass.rnd((int)ContainerPrivateType.__NUM);

        Plugin.ModLog($"ModifyContainer {container.Name} {containerLV}", PrivateLogLevel.Debug);
        container.things.Clear();

        if ((container.trait as TraitContainer).IsFridge)
        {
            if (EClass.rnd(2) == 0)
            {
                containerType = ContainerPrivateType.food_raw;
            }
            else
            {
                containerType = ContainerPrivateType.food;
            }
        }
        else if (container.trait is TraitContainerBook)
        {
            containerType = ContainerPrivateType.book;
        }
        if (EClass.rnd(10000) < Math.Min(100 + EClass.pc.LUC, 5000))
        {
            containerType = ContainerPrivateType.troll;
            generatedNum = container.things.GridSize;
        }

        for (int i = 0; i < generatedNum; i++)
        {
            string targetName;
            int type;
            Card generatedCard;
            switch (containerType)
            {
                case ContainerPrivateType.eq:
                case ContainerPrivateType.eq_2:
                case ContainerPrivateType.eq_3:
                    type = EClass.rnd(5);
                    switch (type)
                    {
                        case 0:
                            targetName = "shop_gun";
                            break;
                        case 1:
                            targetName = "shop_healer";
                            break;
                        default:
                            targetName = "eq";
                            break;
                    }
                    generatedCard = ThingGen.CreateFromFilter(targetName, containerLV);
                    break;
                case ContainerPrivateType.food:
                    type = EClass.rnd(5);
                    switch (type)
                    {
                        case 0:
                            targetName = "shop_booze";
                            break;
                        case 1:
                            targetName = "shop_drink";
                            break;
                        case 2:
                            targetName = "shop_bread";
                            break;
                        default:
                            targetName = "shop_food";
                            break;
                    }
                    generatedCard = ThingGen.CreateFromFilter(targetName, containerLV).SetNum(EClass.rnd(10));
                    break;
                case ContainerPrivateType.food_raw:
                    type = EClass.rnd(4);
                    switch (type)
                    {
                        case 0:
                            targetName = "shop_fruit";
                            break;
                        case 1:
                            targetName = "shop_fish";
                            break;
                        default:
                            targetName = "shop_meat";
                            break;
                    }
                    generatedCard = ThingGen.CreateFromFilter(targetName, containerLV).SetNum(EClass.rnd(20));
                    break;
                case ContainerPrivateType.book:
                    generatedCard = ThingGen.CreateFromFilter("shop_magic", containerLV);
                    break;
                case ContainerPrivateType.black_market:
                    generatedCard = ThingGen.CreateFromFilter("shop_blackmarket", containerLV);
                    break;
                case ContainerPrivateType.furniture:
                    generatedCard = ThingGen.CreateFromFilter("shop_furniture", containerLV);
                    break;
                case ContainerPrivateType.steal:
                    generatedCard = ThingGen.CreateFromFilter("steal", containerLV).SetNum(EClass.rnd(5));
                    break;
                case ContainerPrivateType.troll:
                    // Wikipedia: https://en.wikipedia.org/wiki/Feces
                    // Feces (or faeces; sg.: faex) are the solid or semi-solid remains of food that was not digested in the small intestine,
                    // and has been broken down by bacteria in the large intestine. Feces contain a relatively small amount of metabolic
                    // waste products such as bacterially altered bilirubin, and dead epithelial cells from the lining of the gut.
                    generatedCard = ThingGen.Create("poop", -1, containerLV).SetNum(EClass.rnd(1000));
                    break;
                default:
                    generatedCard = ThingGen.CreateFromFilter("container_general", containerLV);
                    break;
            }
            if (generatedCard == null)
            {
                Plugin.ModLog($"GeneratedCard is null", PrivateLogLevel.Warning);
                continue;
            }
            container.AddCard(CardAddTag(generatedCard));
        }
    }

    public void ModifyUserMap(Zone __instance)
    {
        List<Chara> generatedCharas = new List<Chara>();

        Plugin.ModLog("ModifyUserMap");

        // This may happen when player save & load in the middle of invasion
        if (Initialized)
        {
            Plugin.ModLog("Already initialized, skip.");
            return;
        }
        Initialized = true;

        // Add deleting tag on existing things on the map
        foreach(Thing thing in __instance.map.things)
        {
            CardAddTag(thing);
        }

        // Modify charas
        foreach(Chara chara in __instance.map.charas)
        {
            if (!chara.IsPCFactionOrMinion && !chara.IsPC)
            {
                ModifyChara(__instance, chara);
            }
            else if (!chara.IsPC)
            {
                SpawnList spawnList = SpawnList.Get("c_dungeon", null, null);
                Chara generatedChara = CharaGen.CreateFromFilter(spawnList, EClass.rndHalf(chara.LV), -1);
                generatedCharas.Add(generatedChara);
            }
        }
        Plugin.ModLog($"GeneratedCharas: {generatedCharas.Count}");
        foreach(Chara generatedChara in generatedCharas)
        {
            __instance.AddCard(generatedChara, __instance.map.GetRandomPoint());
            Plugin.ModLog($"GeneratedChara: {generatedChara.Name} {generatedChara.LV}", PrivateLogLevel.Debug);
            ModifyChara(__instance, generatedChara);
        }
        Plugin.ModLog($"MaxEnemyNum: {maxEnemyNum}");
        Plugin.ModLog($"MaxLV: {maxLV}");
        
        // Regenerate all things in containers and add deleting tag on them
        userMapCharaLVList = userMapCharaLVList.OrderByDescending(x => x).ToList();
        foreach(Thing container in __instance.map.ListThing<TraitContainer>())
        {
            userMapContainerList.Add(container);
        }
        userMapContainerList = userMapContainerList.OrderBy(x => Guid.NewGuid()).ToList();
        for (int i = 0; i < userMapContainerList.Count; i++)
        {
            ModifyContainer(userMapContainerList[i], i);
        }

        __instance.isDeathLocation = false;
        EClass.player.ModKarma(-Math.Min((int)Mathf.Sqrt(maxEnemyNum), 10));
    }

    public void OnSuccessExit()
    {
        Msg.SetColor(Msg.colors.TalkGod);
        Msg.SayRaw("Invasion SUCCESS!! ");
		foreach (Chara chara in EClass.pc.party.members)
		{
			ThingContainer things = chara.things;
			Action<Thing> action = delegate(Thing t)
            {
                if (t.GetBool(10086))
                {
                    t.SetBool(10086, false);
                }
            };
			things.Foreach(action, true);
		}
    }

    public void OnKillAll()
    {
        isTotalSuccessTriggered = true;
        Msg.SetColor(Msg.colors.TalkGod);
        Msg.SayRaw("All enemies are killed!! ");
        EClass.Sound.StopBGM(2f, false);
		SE.Play("kill_boss");
        EClass._zone.SetBGM(114, true);
		EClass.player.ModFame(Math.Min(1000, EClass.rndHalf(30 + maxLV)));
		EClass.player.willAutoSave = true;
    }

    public void OnFailExit()
    {
        Msg.SetColor(Msg.colors.Ono);
        Msg.SayRaw("Invasion FAIL!! ");
		List<Thing> list = new List<Thing>();
		foreach (Chara chara in EClass.pc.party.members)
		{
			ThingContainer things = chara.things;
			Action<Thing> action = delegate(Thing t)
            {
                if (t.GetBool(10086))
                {
                    list.Add(t);
                }
            };
			things.Foreach(action, true);
		}
        if (list.Count > 0)
		{
            Msg.SayRaw("You lost everything you found...... ");
			foreach (Thing thing in list)
			{
				thing.Destroy();
			}
		}
    }
}