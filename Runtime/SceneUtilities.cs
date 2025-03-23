#if UNITY_EDITOR
using UnityEditor;

namespace DavidUtils
{
    public static class SceneUtilities
    {
        public static void RepaintAll() => SceneView.RepaintAll();
    }
}
#endif
