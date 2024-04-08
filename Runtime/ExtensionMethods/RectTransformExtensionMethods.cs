using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
    public static class RectTransformExtensionMethods
    {
        // [Width, Height]
        // BotLeft -> TopRight
        public static Vector2 Size(this RectTransform rectT)
        {
            var corners = rectT.Corners();
            return corners.topRight - corners.bottomLeft;
        }

        public static float Width(this RectTransform rectT) => rectT.Size().x;
        public static float Height(this RectTransform rectT) => rectT.Size().y;

        public static float Extent(this RectTransform rectT) => rectT.Size().magnitude;

        // PIVOT
        public static Vector2 PivotLocal(this RectTransform rectT) => rectT.NormalizedToLocalPoint(rectT.pivot);
        public static Vector2 PivotGlobal(this RectTransform rectT) => rectT.position;

        // CORNERS
        public static RectCorners Corners(this RectTransform rectT)
        {
            var corners = new Vector3[4];
            rectT.GetWorldCorners(corners);
            return new RectCorners
            {
                bottomLeft = corners[0],
                topLeft = corners[1],
                topRight = corners[2],
                bottomRight = corners[3]
            };
        }

        // ============================= POINT CONVERSIONS =============================
        public static Vector2 LocalToNormalizedPoint(
            this RectTransform rectT,
            Vector2 localPoint
        ) => localPoint / rectT.Size();

        public static Vector2 NormalizedToLocalPoint(
            this RectTransform rectT,
            Vector2 normalizedPoint
        ) => normalizedPoint * rectT.Size();


        public static Vector2 ScreenToGlobalPoint(this RectTransform rectT, Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectT,
                screenPos,
                null,
                out var worldPos
            );
            return worldPos;
        }

        public static Vector2 ScreenToLocalPoint(this RectTransform rectT, Vector2 screenPos)
            => rectT.ScreenToGlobalPoint(screenPos) - rectT.Corners().bottomLeft;

        public static Vector2 ScreenToNormalizedPoint(
            this RectTransform rectT,
            Vector2 screenPos
        ) => rectT.LocalToNormalizedPoint(rectT.ScreenToLocalPoint(screenPos));

        // CORNERs global positions
        public struct RectCorners
        {
            public Vector2 topLeft;
            public Vector2 topRight;
            public Vector2 bottomLeft;
            public Vector2 bottomRight;
        }
    }
}