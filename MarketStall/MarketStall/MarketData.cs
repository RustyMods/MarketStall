using System;

namespace MarketStall.MarketStall;

public static class MarketData
{
    [Serializable]
    public class MarketTradeItem
    {
        public string m_prefab = null!;
        public int m_price;
        public int m_stack;
        public int m_quality;
        public long m_crafterID = 0L;
        public string m_crafter = "";
        public string m_currency = null!;
        public string m_customData = "";
        public string m_merchant = "";
    }
}