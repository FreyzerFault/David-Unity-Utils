using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.MouseInput
{
    public static class MouseInputUtils
    {
        public static Vector3 MousePosition => Input.mousePosition;
        
        // Checkea límites de la Ventana (Screen).
        // Note: No funciona bien en el editor si activamos Gizmos en varias ventanas (Scene, Game)
        // Se ejecuta una vez por cada ventana donde tengamos Gizmos
        public static bool MouseInScreen()
        {
            Vector3 p = MousePosition;
            return p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height;
        }

        /// <summary>
        /// Checkea si el Mouse apunta a un área 2D desde una vista CENITAL (es decir, un Quad en el plano XZ)
        /// </summary>
        public static bool MouseInArea_CenitalView(Vector3 originPos, Vector2 size, out Vector2 normalizedPos)
        {
            normalizedPos = Vector2.zero;
            if (Camera.main == null) return false;
            
            Vector3 mousePos = MousePosition;
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos).WithY(0);
            
            normalizedPos = (worldMousePos - originPos).ToVector2xz() / size;
            
            return normalizedPos.IsNormalized();
        }
        
        /// <summary>
        /// Checkea si el Mouse apunta a un área 2D desde una vista FRONTAL (es decir, un Quad en el plano XY)
        /// </summary>
        public static bool MouseInArea_FrontView(Vector3 originPos, Vector2 size, out Vector2 normalizedPos)
        {
            normalizedPos = Vector2.zero;
            if (Camera.main == null) return false;
            
            Vector3 mousePos = MousePosition;
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos).WithZ(0);
            
            normalizedPos = (worldMousePos - originPos).ToVector2xy() / size;
            
            return normalizedPos.IsNormalized();
        }
    }
}
