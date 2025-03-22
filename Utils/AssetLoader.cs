using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EditorEnhanced.Utils;

internal static class AssetLoader
{
    private static Dictionary<string, AssetBundle> _loaded = new Dictionary<string, AssetBundle>();
    
    public static AssetBundle LoadFromResource(string resourcePath)
    {
        if (_loaded.TryGetValue(resourcePath, out var resource))
        {
            return resource;
        }
        _loaded[resourcePath] = AssetBundle.LoadFromMemory(GetResource(Assembly.GetCallingAssembly(), resourcePath));
        return _loaded[resourcePath];
    }

    private static byte[] GetResource(Assembly assembly, string resourcePath)
    {
        var manifestResourceStream = assembly.GetManifestResourceStream(resourcePath);
        var array = new byte[manifestResourceStream.Length];
        manifestResourceStream.Read(array, 0, (int)manifestResourceStream.Length);
        return array;
    }
}