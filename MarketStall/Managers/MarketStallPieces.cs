using MarketStall.MarketStall;

namespace MarketStall.Managers;

public static class MarketStallPieces
{
    public static void InitPieces()
    {
        // Globally turn off configuration options for your pieces, omit if you don't want to do this.
        BuildPiece.ConfigurationEnabled = true;

        BuildPiece MarketStall = new("marketstallbundle2", "MarketStall");
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
        
        BuildPiece MarketStall2 = new("marketstallbundle2", "MarketStall_Small");
        MarketStall2.Name.English("Market Stall Mini");
        MarketStall2.Description.English("Personal marketplace");
        MarketStall2.RequiredItems.Add("FineWood", 20, true);
        MarketStall2.RequiredItems.Add("Wood", 20, true);
        MarketStall2.RequiredItems.Add("Thunderstone", 1, true);
        MarketStall2.RequiredItems.Add("Bronze", 5, true);
        MarketStall2.Category.Set(BuildPieceCategory.Misc);
        MarketStall2.Crafting.Set(CraftingTable.None);
        MaterialReplacer.RegisterGameObjectForMatSwap(MarketStall2.Prefab);
        PieceEffectManager.PrefabsToSet.Add(MarketStall2.Prefab);
        MarketStall2.Prefab.AddComponent<Market>();
    }
}