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
        
        
        // GUI STYLES for Editor
        public static GUIStyle MonoStyle = MonoFont.ToGUIStyle();
        public static GUIStyle SmallMonoStyle = MonoFont.ToGUIStyle(SmallFontSize);
        
        public static GUIStyle ToGUIStyle(this Font font, int fontSize = -1) =>
            new() { font = font, fontSize = fontSize == -1 ? StandardFontSize : fontSize };
    }
}
