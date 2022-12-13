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
    }
}