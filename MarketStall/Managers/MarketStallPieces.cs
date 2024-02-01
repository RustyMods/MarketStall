using MarketStall.MarketStall;

namespace MarketStall.Managers;

public static class MarketStallPieces
{
    public static void InitPieces()
    {
        // Globally turn off configuration options for your pieces, omit if you don't want to do this.
        BuildPiece.ConfigurationEnabled = true;

        BuildPiece MarketStall = new("marketstallbundle", "MarketStall");
        MarketStall.Name.English("Market Stall");
        MarketStall.Description.English("Personal marketplace");
        MarketStall.RequiredItems.Add("FineWood", 20, true);
        MarketStall.RequiredItems.Add("Wood", 20, true);
        MarketStall.RequiredItems.Add("Thunderstone", 1, true);
        MarketStall.RequiredItems.Add("Bronze", 5, true);
        MarketStall.Category.Set(BuildPieceCategory.Misc);
        MarketStall.Crafting.Set(CraftingTable.None);
        MaterialReplacer.RegisterGameObjectForMatSwap(MarketStall.Prefab);
        PieceEffectManager.PrefabsToSet.Add(MarketStall.Prefab);
        MarketStall.Prefab.AddComponent<Market>();
    }
}