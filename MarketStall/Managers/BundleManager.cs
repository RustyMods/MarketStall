using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MarketStall;

public static class BundleManager
{
    private static readonly Dictionary<string, AssetBundle> bundles = new();

    public static T Load<T>(string bundle, string file) where T : Object => GetAssetBundle(bundle).LoadAsset<T>(file);
    
    private static AssetBundle GetAssetBundle(string fileName)
    {
        if (bundles.TryGetValue(fileName, out var assetBundle)) return assetBundle;
        Assembly execAssembly = Assembly.GetExecutingAssembly();
        string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
        using Stream stream = execAssembly.GetManifestResourceStream(resourceName);
        var result = AssetBundle.LoadFromStream(stream);
        if (result != null)
        {
            bundles[fileName] = result;
        }

        return result;
    }
}