using UnityEngine;

namespace MarketStall.Utility;

public static class Methods
{
    private static GameObject Coins = null!;
    
    public static GameObject TryGetPrefab(string input, string defaultItem = "Coins")
    {
       GameObject prefab = ObjectDB.instance.GetItemPrefab(input);
       if (prefab) return prefab;
       if (!Coins) Coins = ObjectDB.instance.GetItemPrefab(defaultItem);
       return Coins;
    }
    
    
}