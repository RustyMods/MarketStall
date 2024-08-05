using System.Collections.Generic;
using MarketStall.MarketStall;
using PieceManager;

namespace MarketStall.Managers;

public static class MarketStallPieces
{
    private static readonly List<string> m_pieceNames = new();
    private static readonly List<string> m_communityPieceNames = new();
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
        MaterialReplacer.RegisterGameObjectForMatSwap(MarketStall.Prefab.transform.Find("models/replace").gameObject);
        MaterialReplacer.RegisterGameObjectForShaderSwap(MarketStall.Prefab.transform.Find("models/no_replace").gameObject, MaterialReplacer.ShaderType.PieceShader);
        MarketStall.PlaceEffects = new() { "vfx_Place_stone_wall_2x1", "sfx_build_hammer_stone" };
        MarketStall.DestroyedEffects = new() { "vfx_SawDust", "sfx_wood_destroyed" };
        MarketStall.HitEffects = new() { "vfx_SawDust" };
        MarketStall.Prefab.AddComponent<Market>();
        m_pieceNames.Add(MarketStall.Prefab.name);
        
        BuildPiece MarketStall2 = new("marketstallbundle2", "MarketStall_Small");
        MarketStall2.Name.English("Market Stall Mini");
        MarketStall2.Description.English("Personal marketplace");
        MarketStall2.RequiredItems.Add("FineWood", 20, true);
        MarketStall2.RequiredItems.Add("Wood", 20, true);
        MarketStall2.RequiredItems.Add("Thunderstone", 1, true);
        MarketStall2.RequiredItems.Add("Bronze", 5, true);
        MarketStall2.Category.Set(BuildPieceCategory.Misc);
        MarketStall2.Crafting.Set(CraftingTable.None);
        MaterialReplacer.RegisterGameObjectForMatSwap(MarketStall2.Prefab.transform.Find("models/replace").gameObject);
        MaterialReplacer.RegisterGameObjectForShaderSwap(MarketStall2.Prefab.transform.Find("models/no_replace").gameObject, MaterialReplacer.ShaderType.PieceShader);
        MarketStall2.PlaceEffects = new() { "vfx_Place_stone_wall_2x1", "sfx_build_hammer_stone" };
        MarketStall2.DestroyedEffects = new() { "vfx_SawDust", "sfx_wood_destroyed" };
        MarketStall2.HitEffects = new() { "vfx_SawDust" };
        MarketStall2.Prefab.AddComponent<Market>();
        m_pieceNames.Add(MarketStall2.Prefab.name);
        
        BuildPiece CommunityMarketStall = new("marketstallbundle2", "MarketStall_Global");
        CommunityMarketStall.Name.English("Community Market Stall");
        CommunityMarketStall.Description.English("Community marketplace");
        CommunityMarketStall.RequiredItems.Add("FineWood", 20, true);
        CommunityMarketStall.RequiredItems.Add("Wood", 20, true);
        CommunityMarketStall.RequiredItems.Add("Thunderstone", 1, true);
        CommunityMarketStall.RequiredItems.Add("Bronze", 5, true);
        CommunityMarketStall.Category.Set(BuildPieceCategory.Misc);
        CommunityMarketStall.Crafting.Set(CraftingTable.None);
        MaterialReplacer.RegisterGameObjectForMatSwap(CommunityMarketStall.Prefab.transform.Find("models/replace").gameObject);
        MaterialReplacer.RegisterGameObjectForShaderSwap(CommunityMarketStall.Prefab.transform.Find("models/no_replace").gameObject, MaterialReplacer.ShaderType.PieceShader);
        CommunityMarketStall.PlaceEffects = new() { "vfx_Place_stone_wall_2x1", "sfx_build_hammer_stone" };
        CommunityMarketStall.DestroyedEffects = new() { "vfx_SawDust", "sfx_wood_destroyed" };
        CommunityMarketStall.HitEffects = new() { "vfx_SawDust" };
        CommunityMarketStall.Prefab.AddComponent<CommunityMarket>();
        m_communityPieceNames.Add(CommunityMarketStall.Prefab.name);
        CommunityMarketStall.SpecialProperties = new SpecialProperties()
        {
            AdminOnly = true
        };
    }

    public static bool isMarketStall(string name) => m_pieceNames.Contains(name);
    public static bool isCommunityMarketStall(string name) => m_communityPieceNames.Contains(name);
}