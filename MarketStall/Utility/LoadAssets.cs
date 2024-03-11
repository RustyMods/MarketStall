using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MarketStall.Utility;

public static class LoadAssets
{
    public static AssetBundle _assets = null!;

    public static void InitAssetBundle()
    {
        _assets = GetAssetBundle("marketstallbundle2");
    }

    private static AssetBundle GetAssetBundle(string fileName)
    {
        Assembly execAssembly = Assembly.GetExecutingAssembly();
        string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
        using Stream? stream = execAssembly.GetManifestResourceStream(resourceName);
        return AssetBundle.LoadFromStream(stream);
    }
}