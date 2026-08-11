using System.Linq;
using HarmonyLib;
using PieceManager;
using UnityEngine;

namespace MarketStall.Managers;

public static class PrefabManager
{
    private static ZNetScene _scene;
    private static ObjectDB _db;

    public static bool TryGetPrefab(string prefabName, out GameObject prefab)
    {
        prefab = null;

        if (ZNetScene.instance)
        {
            prefab = ZNetScene.instance.GetPrefab(prefabName);
        }
        else
        {
            prefab = _scene.m_prefabs.FirstOrDefault(x => x.name == prefabName);
        }
        return prefab != null;
    }

    public static void Register(GameObject prefab)
    {
        if (prefab.GetComponent<ItemDrop>())
        {
            if (ObjectDB.instance)
            {
                ObjectDB.instance.m_items.Add(prefab);
                ObjectDB.instance.m_itemByHash[prefab.name.GetStableHashCode()] = prefab;
            }
            else
            {
                _db.m_items.Add(prefab);
            }
        }
        
        if (!prefab.GetComponent<ZNetView>()) return;
        if (ZNetScene.instance)
        {
            if (ZNetScene.instance.m_prefabs.Contains(prefab)) return;
            ZNetScene.instance.m_prefabs.Add(prefab);
            ZNetScene.instance.m_namedPrefabs[prefab.name.GetStableHashCode()] = prefab;
        }
        else
        {
            if (_scene.m_prefabs.Contains(prefab)) return;
            _scene.m_prefabs.Add(prefab);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Patch
    {
        private static void Prefix(FejdStartup __instance)
        {
            Patch_FejdStartup(__instance);
        }
    }

    public static bool m_loaded;
    public static void Patch_FejdStartup(FejdStartup __instance)
    {
        _scene = __instance.m_objectDBPrefab.GetComponent<ZNetScene>();
        _db = __instance.m_objectDBPrefab.GetComponent<ObjectDB>();

        if (!m_loaded)
        {
            GameObject root = new GameObject("root");
            root.SetActive(false);
            Object.DontDestroyOnLoad(root);

            if (!TryGetPrefab("Haldor", out GameObject haldor)) return;

            var defaultGo = new GameObject("MarketStall_NPC");
            defaultGo.transform.SetParent(root.transform);
            var g = Object.Instantiate(haldor, defaultGo.transform);
            foreach (var c in g.GetComponents(typeof(MonoBehaviour)))
            {
                Object.Destroy(c);
            }
            g.name = "Haldor";
            
            
            var defaultPiece = defaultGo.AddComponent<Piece>();
            defaultPiece.m_icon = BundleManager.Load<Sprite>("marketstall_bundle", "Haldor_icon.png");

            defaultGo.AddComponent<Stall>();
            defaultGo.AddComponent<ZNetView>().m_persistent = true;
            BuildPiece defaultBuild = new BuildPiece(defaultGo);
            defaultBuild.Name.English("Custom Stall");
            defaultBuild.Description.English("Personal marketplace");
            defaultBuild.RequiredItems.Add("FineWood", 20, true);
            defaultBuild.RequiredItems.Add("Wood", 20, true);
            defaultBuild.RequiredItems.Add("Thunderstone", 1, true);
            defaultBuild.RequiredItems.Add("Bronze", 5, true);
            defaultBuild.Category.Set(BuildPieceCategory.Misc);
            defaultBuild.Crafting.Set(CraftingTable.None);
            defaultGo.AddComponent<NPCSelect>();
            
            

            m_loaded = true;
        }
        
        
        BuildPiece.Patch_FejdStartup(__instance);
    }
}