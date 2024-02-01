using System.Collections.Generic;
using HarmonyLib;
using MarketStall.UI;
using UnityEngine;

namespace MarketStall.Managers;

public static class PieceEffectManager
{
    public static readonly List<GameObject> PrefabsToSet = new();
    
    private static void AddEffects(ZNetScene instance)
    {
        if (!instance) return;
        foreach (GameObject prefab in PrefabsToSet)
        {
            GameObject Object = instance.GetPrefab(prefab.name);
            if (!Object) continue;
            SetPieceScript(instance, Object, "vfx_Place_stone_wall_2x1", "sfx_build_hammer_stone");
        }
    }
    
    private static void SetPieceScript(ZNetScene scene, GameObject prefab, string placementEffectName1, string placementEffectName2)
        {
        GameObject placeEffect1 = scene.GetPrefab(placementEffectName1);
        GameObject placeEffect2 = scene.GetPrefab(placementEffectName2);
        if (!placeEffect1 || !placeEffect2) return;
        EffectList placementEffects = new EffectList()
        {
            m_effectPrefabs = new[]
            {
                new EffectList.EffectData()
                {
                    m_prefab = placeEffect1,
                    m_enabled = true,
                    m_variant = -1,
                    m_attach = false,
                    m_inheritParentRotation = false,
                    m_inheritParentScale = false,
                    m_randomRotation = false,
                    m_scale = false,
                    m_childTransform = ""
                },
                new EffectList.EffectData()
                {
                    m_prefab = placeEffect2,
                    m_enabled = true,
                    m_variant = -1,
                    m_attach = false,
                    m_inheritParentRotation = false,
                    m_inheritParentScale = false,
                    m_randomRotation = false,
                    m_scale = false,
                    m_childTransform = ""
                }
            }
        };
        Piece pieceScript = prefab.GetComponent<Piece>();
        pieceScript.m_placeEffect = placementEffects;
        // Configure piece placement restrictions
        pieceScript.m_groundPiece = false;
        pieceScript.m_allowAltGroundPlacement = false;
        pieceScript.m_cultivatedGroundOnly = false;
        pieceScript.m_waterPiece = false;
        pieceScript.m_clipGround = true;
        pieceScript.m_clipEverything = false;
        pieceScript.m_noInWater = false;
        pieceScript.m_notOnWood = false;
        pieceScript.m_notOnTiltingSurface = false;
        pieceScript.m_inCeilingOnly = false;
        pieceScript.m_notOnFloor = false;
        pieceScript.m_noClipping = false;
        pieceScript.m_onlyInTeleportArea = false;
        pieceScript.m_allowedInDungeons = false;
        pieceScript.m_spaceRequirement = 0f;
        pieceScript.m_repairPiece = false;
        pieceScript.m_canBeRemoved = true;
        pieceScript.m_allowRotatedOverlap = false;
        pieceScript.m_vegetationGroundOnly = false;
        }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class SetPiecesEffects
    {
        private static void Postfix(ZNetScene __instance)
        {
            if (!__instance) return;
            AddEffects(__instance);
            CacheAssets.CargoCrate = CacheAssets.GetDestroyedLootPrefab(__instance);
        }
    }
}