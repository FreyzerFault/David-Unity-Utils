using TMPro;
using UnityEngine;

namespace DavidUtils.UI.Text
{
    [CreateAssetMenu(fileName = "Text Block", menuName = "Utils/Text/Text Block")]
    public class TextBlock : ScriptableObject
    {
        public string text =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat";
        public int size = -1; // -1 means autoSize = true
        public Color color = Color.white;
        public TMP_FontAsset fontAsset;
    }
}