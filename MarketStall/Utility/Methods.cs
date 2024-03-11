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

    public static void ShowMessage(Humanoid player, string message, Sprite? sprite = null)
    {
        player.Message(
            MessageHud.MessageType.Center,
            new string('\n', MarketStallPlugin._MessageIncrement.Value) 
            + $"<color={GetColor(MarketStallPlugin._MessageColor.Value)}>" 
            + Localization.instance.Localize(message) 
            + "</color>", 0, sprite);
    }

    private static string GetColor(MarketStallPlugin.MessageColor color)
    {
        return (color) switch
        {
            MarketStallPlugin.MessageColor.White => "white",
            MarketStallPlugin.MessageColor.Orange => "orange",
            MarketStallPlugin.MessageColor.Red => "red",
            MarketStallPlugin.MessageColor.Green => "green",
            MarketStallPlugin.MessageColor.Yellow => "yellow",
            _ => "white",
        };
    }
    
}