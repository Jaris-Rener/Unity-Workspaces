namespace Howl.Workspaces
{
    using System;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public abstract class WorkspaceItem
    {
        [JsonIgnore]
        public virtual WorkspaceGroup ParentGroup
        {
            get => _parentGroup;
            set => _parentGroup = value;
        }

        public bool Locked;

        public string ColorString = ColorUtility.ToHtmlStringRGB(new(0.3f, 0.3f, 0.3f, 1f));

        [JsonIgnore] public Color Color
        {
            get =>
                ColorUtility.TryParseHtmlString($"#{ColorString}", out var color)
                    ? color
                    : Color.white;
            set => ColorString = ColorUtility.ToHtmlStringRGB(value);
        }

        [JsonProperty] private float _positionX;
        [JsonProperty] private float _positionY;
        [JsonProperty] private WorkspaceGroup _parentGroup;

        protected static readonly Color GreyColor = new(0.3f, 0.3f, 0.3f, 1f);
        protected static readonly Color RedColor = Color.red;
        protected static readonly Color GreenColor = Color.green;
        protected static readonly Color BlueColor = new(0f, 0.5f, 1f);
        protected static readonly Color PinkColor = new(1f, 0.5f, 1f);
        protected static readonly Color PurpleColor = new(0.6f, 0.1f, 1f);
        protected static readonly Color OrangeColor = new(1f, 0.5f, 0f);
        protected static readonly Color YellowColor = Color.yellow;

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

        [JsonIgnore] public virtual int RenderLayer => 0;
        [JsonIgnore] public virtual GUIContent ToolbarContent => GUIContent.none;

        public virtual Rect GetRect(Vector2 scale) => new(Position, scale);
        public virtual Rect GetDragRect(Vector2 scale) => new(Position, scale);

        public abstract void Render(Workspace workspace, Vector2 renderScale, bool selected, GUISkin skin);
        public abstract GenericMenu GetGenericMenu(Workspace workspace);

        public virtual void OnDoubleClick() { }

        public virtual void OnCtrlClick() { }
    }
}