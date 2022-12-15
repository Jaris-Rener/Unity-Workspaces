namespace Howl.Workspaces
{
    using System;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class WorkspaceItemRenderer
    {
        public static void Render(this WorkspaceItem item, Vector2 renderScale, bool selected)
        {
            if (!item.IsValid)
                return;

            var icon = item.GetIcon();

            var boxRect = new Rect(item.Position, renderScale);
            GUI.color = item.Color;
            GUI.Box(boxRect, GUIContent.none, selected ? "TE NodeBoxSelected" : "TE NodeBox");
            GUI.color = Color.white;

            var padding = new Vector2(8, 8);
            var textureRect = new Rect(boxRect);
            textureRect.size -= padding*2;
            textureRect.position += padding;
            GUI.DrawTexture(textureRect, icon);

            if (item.Locked)
            {
                var iconSize = 15;
                var rect = new Rect(boxRect.xMax - iconSize*0.75f, boxRect.y - iconSize*0.25f, iconSize, iconSize);
                GUI.DrawTexture(rect, EditorGUIUtility.FindTexture("d_AssemblyLock"), ScaleMode.ScaleToFit);
            }

            var labelRect = new Rect(boxRect);
            labelRect.height = EditorGUIUtility.singleLineHeight;
            labelRect.y = boxRect.yMax;
            GUI.Label(labelRect, item.Name);
        }
    }

    [Serializable]
    public class WorkspaceItem
    {
        public string AssetGuid;
        public string ColorString;
        public bool Locked;

        [JsonProperty] private float _positionX;
        [JsonProperty] private float _positionY;

        [JsonIgnore] public Color Color
        {
            get =>
                ColorUtility.TryParseHtmlString($"#{ColorString}", out var color)
                    ? color
                    : Color.white;
            set => ColorString = ColorUtility.ToHtmlStringRGB(value);
        }

        [JsonIgnore] public string Name => GetObject().name;

        [JsonIgnore]
        public Vector2 Position
        {
            get => new(_positionX, _positionY);
            set
            {
                _positionX = value.x;
                _positionY = value.y;
            }
        }

        public bool IsValid => GetObject() != null;

        public Rect GetRect(Vector2 scale) => new(Position, scale);
        public override string ToString() => Name;

        public Object GetObject()
            => AssetDatabase.LoadAssetAtPath<Object>(GetPath());

        public string GetPath()
            => AssetDatabase.GUIDToAssetPath(AssetGuid);

        public Texture GetIcon()
            => AssetPreview.GetMiniThumbnail(GetObject());
    }
}