using UnityEngine;

namespace MarketStall.UI;

public static class CacheAssets
{
    public static GameObject? CargoCrate = null!;
    
    public static GameObject? GetDestroyedLootPrefab(ZNetScene instance)
    {
        if (!instance) return null;
        GameObject karve = ZNetScene.instance.GetPrefab("Karve");
        Container? karveContainer = karve.GetComponentInChildren<Container>();
        if (!karveContainer) return null;
        GameObject Crate = karveContainer.m_destroyedLootPrefab;
        if (!Crate) return null;
        return Crate;
    }
}