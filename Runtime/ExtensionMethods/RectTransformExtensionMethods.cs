using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
    public static class RectTransformExtensionMethods
    {
        // BotLeft -> TopRight
        // Also works as REAL [Width, Height] on world coords
        public static Vector2 Diagonal(this RectTransform rectT)
        {
            var corners = rectT.Corners();
            return corners.TopRight - corners.BottomLeft;
        }

        // Scaled size (real en pantalla)
        public static float Size(this RectTransform rectT) => rectT.Diagonal().magnitude;

        // PIVOT
        public static Vector2 PivotLocal(this RectTransform rectT) => rectT.Size() * rectT.pivot;

        public static Vector2 PivotGlobal(this RectTransform rectT) => rectT.position;

        public static RectCorners Corners(this RectTransform rectT)
        {
            var corners = new Vector3[4];
            rectT.GetWorldCorners(corners);
            return new RectCorners
            {
                BottomLeft = corners[0],
                TopLeft = corners[1],
                TopRight = corners[2],
                BottomRight = corners[3]
            };
        }

        // ============================= POINT CONVERSIONS =============================
        public static Vector2 LocalToNormalizedPoint(
            this RectTransform rectT,
            Vector2 localPoint
        ) => localPoint / (rectT.rect.size * rectT.localScale);

        public static Vector2 NormalizedToLocalPoint(
            this RectTransform rectT,
            Vector2 normalizedPoint
        ) => normalizedPoint * rectT.rect.size * rectT.localScale;

        public static Vector2 ScreenToLocalPoint(this RectTransform rectT, Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectT,
                screenPos,
                null,
                out var localPos
            );
            return localPos
                   // Escalado
                   * rectT.localScale
                   // Sumado al pivot para que el punto sea relativo al la esquina inferior izquierda
                   + rectT.PivotLocal();
        }

        public static Vector2 ScreenToNormalizedPoint(
            this RectTransform rectT,
            Vector2 screenPos
        ) => rectT.LocalToNormalizedPoint(rectT.ScreenToLocalPoint(screenPos));

        // CORNERs global positions
        public struct RectCorners
        {
            public Vector2 TopLeft;
            public Vector2 TopRight;
            public Vector2 BottomLeft;
            public Vector2 BottomRight;
        }
    }
}