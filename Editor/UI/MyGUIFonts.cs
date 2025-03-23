using UnityEngine;
using DavidUtils.UI.Fonts;

namespace Editor.UI
{
    public static class MyGUIFonts
    {
        // GUI STYLES for Editor
        public static GUIStyle monoStyle = MyFonts.MonoFont.ToGUIStyle();
        public static readonly GUIStyle SmallMonoStyle = MyFonts.MonoFont.ToGUIStyle(MyFonts.SmallFontSize);
        
        public static GUIStyle ToGUIStyle(this Font font, int fontSize = -1) =>
            new() { font = font, fontSize = fontSize == -1 ? MyFonts.StandardFontSize : fontSize };
    }
}
