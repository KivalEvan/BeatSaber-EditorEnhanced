using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EditorEnhanced.Utils;

public static class AssetLoader
{
   internal const string ModelResourcePath = "model";

   private static readonly Dictionary<string, AssetBundle> _loaded = new();

   public static AssetBundle LoadFromResource(string resourcePath)
   {
      if (_loaded.TryGetValue(resourcePath, out var resource)) return resource;
      _loaded[resourcePath] = AssetBundle.LoadFromMemory(GetResource(Assembly.GetCallingAssembly(), resourcePath));
      return _loaded[resourcePath];
   }

   public static byte[] GetResource(Assembly assembly, string resourcePath)
   {
      var resourceName = assembly
         .GetManifestResourceNames()
         .FirstOrDefault(name => string.Equals(name, resourcePath, StringComparison.Ordinal)
                                 || name.EndsWith("." + resourcePath, StringComparison.Ordinal));
      if (resourceName == null)
         throw new InvalidOperationException($"Embedded resource '{resourcePath}' was not found.");

      var manifestResourceStream = assembly.GetManifestResourceStream(resourceName);
      if (manifestResourceStream == null)
         throw new InvalidOperationException($"Embedded resource '{resourceName}' could not be opened.");
      var array = new byte[manifestResourceStream.Length];
      manifestResourceStream.Read(array, 0, (int)manifestResourceStream.Length);
      manifestResourceStream.Dispose();
      return array;
   }
}
