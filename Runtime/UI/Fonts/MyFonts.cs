using System.Linq;
using UnityEngine;

namespace DavidUtils.UI.Fonts
{
    public static class MyFonts
    {
        public static readonly Font MonoFont = new(Font.GetPathsToOSFonts().First(f => f.ToLower().Contains("mono")));
        
        public static void LogLoadedFonts() => UnityEngine.Debug.Log($"Loaded Fonts: {MonoFont.name}");
        public static void LogAllOSFonts() => UnityEngine.Debug.Log($"OS Fonts: {string.Join(", ", Font.GetPathsToOSFonts())}");
        
        // Font SIZES
        public static readonly int StandardFontSize = 12;
        public static readonly int SmallFontSize = Mathf.RoundToInt(StandardFontSize * 0.8f);
    }
}
