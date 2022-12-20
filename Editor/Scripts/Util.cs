namespace Howl.Workspaces
{
    using UnityEngine;

    public static class Util
    {
        public static Rect GetNonInvertedRect(this Rect rect)
        {
            if (rect.xMax < rect.xMin)
                (rect.xMin, rect.xMax) = (rect.xMax, rect.xMin);

            if (rect.yMax < rect.yMin)
                (rect.yMin, rect.yMax) = (rect.yMax, rect.yMin);

            return rect;
        }

        public static Rect AddPadding(this Rect rect, float padding)
        {
            rect.x += padding;
            rect.y += padding;
            rect.width -= padding*2f;
            rect.height -= padding*2f;
            return rect;
        }

        public static Rect AddPadding(this Rect rect, Vector4 padding)
        {
            rect.x += padding.x;
            rect.y += padding.y;
            rect.width -= padding.x + padding.z;
            rect.height -= padding.y + padding.w;
            return rect;
        }
    }
}