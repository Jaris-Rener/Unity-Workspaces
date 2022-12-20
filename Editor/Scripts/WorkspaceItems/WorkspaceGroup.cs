namespace Howl.Workspaces
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;

    public class WorkspaceGroup : WorkspaceItem
    {
        public const int ResizePadding = 4;
        public override WorkspaceGroup ParentGroup => null;

        public override int RenderLayer => -10;
        public string Description;

        public override Rect GetRect(Vector2 scale) => Rect;
        public override Rect GetDragRect(Vector2 scale) => Rect.AddPadding(ResizePadding);

        [JsonProperty] private float _sizeX = 192;
        [JsonProperty] private float _sizeY = 128;
        public SpriteAlignment _resizing = SpriteAlignment.Custom;

        [JsonIgnore]
        public Vector2 Size
        {
            get => new(_sizeX, _sizeY);
            set
            {
                _sizeX = value.x;
                _sizeY = value.y;
            }
        }

        [JsonIgnore] public Rect Rect => new(Position, Size);

        public override void Render(Workspace workspace, Vector2 renderScale, bool selected, GUISkin skin)
        {
            Color.RGBToHSV(Color, out var h, out var s, out var v);
            var lightColor = Color.HSVToRGB(h, s*0.8f, v*1.2f);
            GUI.backgroundColor = selected ? lightColor : Color;
            GUI.contentColor = Color.HSVToRGB(h, Mathf.Min(0.325f, s), 1);

            if (selected)
                Description = GUI.TextArea(Rect, Description, skin.GetStyle("CommentBox"));
            else
                GUI.Box(Rect, Description, skin.GetStyle("CommentBox"));

            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;

            if (Locked)
            {
                const int iconSize = 15;
                var rect = new Rect(Rect.xMax - iconSize*0.75f, Rect.y - iconSize*0.25f, iconSize, iconSize);
                GUI.DrawTexture(rect, EditorGUIUtility.FindTexture("d_AssemblyLock"), ScaleMode.ScaleToFit);
            }

            if (!Locked)
                HandleResizing(workspace);
        }

        public override GenericMenu GetGenericMenu(Workspace workspace)
        {
            var menu = new GenericMenu();
            if (Locked)
                menu.AddItem(new GUIContent("Unlock Item"), true, () => workspace.SetItemLock(this, false));
            else
                menu.AddItem(new GUIContent("Lock Item"), false, () => workspace.SetItemLock(this, true));

            menu.AddItem(new GUIContent("Delete group"), false, () => workspace.RemoveItem(this));
            menu.AddItem(new GUIContent("Sort contents"), false, () => LayoutContents(workspace));
            menu.AddItem(new GUIContent("Set color/Grey"), false, () => workspace.SetItemColor(this, GreyColor));
            menu.AddItem(new GUIContent("Set color/Red"), false, () => workspace.SetItemColor(this, RedColor));
            menu.AddItem(new GUIContent("Set color/Orange"), false, () => workspace.SetItemColor(this, OrangeColor));
            menu.AddItem(new GUIContent("Set color/Yellow"), false, () => workspace.SetItemColor(this, YellowColor));
            menu.AddItem(new GUIContent("Set color/Green"), false, () => workspace.SetItemColor(this, GreenColor));
            menu.AddItem(new GUIContent("Set color/Blue"), false, () => workspace.SetItemColor(this, BlueColor));
            menu.AddItem(new GUIContent("Set color/Pink"), false, () => workspace.SetItemColor(this, PinkColor));
            menu.AddItem(new GUIContent("Set color/Purple"), false, () => workspace.SetItemColor(this, PurpleColor));
            return menu;
        }

        private void LayoutContents(Workspace workspace)
        {
            var layoutArea = Rect.AddPadding(new Vector4(8, 32, 8, 8));
            var items = this
                .GetContainedItems(workspace)
                .Where(x => !x.Locked)
                .ToArray();

            workspace.LayoutItems(layoutArea, WorkspaceWindow.ItemScale, items);
        }

        private void HandleResizing(Workspace workspace)
        {
            if (Event.current.type is EventType.MouseUp)
            {
                _resizing = SpriteAlignment.Custom;
            }

            if (Event.current.type is EventType.MouseDrag)
            {
                var delta = Event.current.delta;
                switch (_resizing)
                {
                    case SpriteAlignment.TopCenter:
                        Position += Vector2.up*delta;
                        Size -= Vector2.up*delta;
                        workspace.SetDirty();
                        break;
                    case SpriteAlignment.TopRight:
                        Position += Vector2.up*delta;
                        Size -= new Vector2(-1, 1)*delta;
                        workspace.SetDirty();
                        break;
                    case SpriteAlignment.BottomCenter:
                        Size += Vector2.up*delta;
                        workspace.SetDirty();
                        break;
                    case SpriteAlignment.BottomRight:
                        Size += Vector2.one*delta;
                        workspace.SetDirty();
                        break;
                    case SpriteAlignment.RightCenter:
                        Size += Vector2.right*delta;
                        workspace.SetDirty();
                        break;
                    case SpriteAlignment.BottomLeft:
                        Size -= new Vector2(1, -1)*delta;
                        Position += Vector2.right*delta;
                        workspace.SetDirty();
                        break;
                    case SpriteAlignment.TopLeft:
                        Size -= Vector2.one*delta;
                        Position += Vector2.one*delta;
                        workspace.SetDirty();
                        break;
                    case SpriteAlignment.LeftCenter:
                        Position += Vector2.right*delta;
                        Size -= Vector2.right*delta;
                        workspace.SetDirty();
                        break;
                }
            }

            // Top
            var resizeTop = new Rect(
                Rect.x + ResizePadding,
                Rect.y - ResizePadding,
                Rect.width - ResizePadding*2,
                ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeTop, MouseCursor.ResizeVertical);
            if (resizeTop.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.TopCenter;


            // Top-Right
            var resizeTopRight = new Rect(
                Rect.xMax - ResizePadding,
                Rect.y - ResizePadding,
                ResizePadding*2,
                ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeTopRight, MouseCursor.ResizeUpRight);
            if (resizeTopRight.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.TopRight;

            // Right
            var resizeRight = new Rect(
                Rect.xMax - ResizePadding,
                Rect.y + ResizePadding,
                ResizePadding*2,
                Rect.height - ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeRight, MouseCursor.ResizeHorizontal);
            if (resizeRight.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.RightCenter;

            // Bottom-Right
            var resizeBottomRight = new Rect(
                Rect.xMax - ResizePadding,
                Rect.yMax - ResizePadding,
                ResizePadding*2,
                ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeBottomRight, MouseCursor.ResizeUpLeft);
            if (resizeBottomRight.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.BottomRight;

            // Bottom
            var resizeBottom = new Rect(
                Rect.x + ResizePadding,
                Rect.yMax - ResizePadding,
                Rect.width - ResizePadding*2,
                ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeBottom, MouseCursor.ResizeVertical);
            if (resizeBottom.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.BottomCenter;

            // Bottom-Left
            var resizeBottomLeft = new Rect(
                Rect.x - ResizePadding,
                Rect.yMax - ResizePadding,
                ResizePadding*2,
                ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeBottomLeft, MouseCursor.ResizeUpRight);
            if (resizeBottomLeft.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.BottomLeft;

            // Left
            var resizeLeft = new Rect(
                Rect.x - ResizePadding,
                Rect.y + ResizePadding,
                ResizePadding*2,
                Rect.height - ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeLeft, MouseCursor.ResizeHorizontal);
            if (resizeLeft.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.LeftCenter;


            // Top-Left
            var resizeTopLeft = new Rect(
                Rect.x - ResizePadding,
                Rect.y - ResizePadding,
                ResizePadding*2,
                ResizePadding*2);

            EditorGUIUtility.AddCursorRect(resizeTopLeft, MouseCursor.ResizeUpLeft);
            if (resizeTopLeft.Contains(Event.current.mousePosition))
                if (Event.current.type is EventType.MouseDown)
                    _resizing = SpriteAlignment.TopLeft;

            // GUI.color = new Color(1f, 0f, 0f, 0.25f);
            // GUI.DrawTexture(resizeTop, Texture2D.whiteTexture);
            // GUI.color = new Color(1f, 0f, 0f, 0.25f);
            // GUI.DrawTexture(resizeRight, Texture2D.whiteTexture);
            // GUI.color = new Color(1f, 0f, 0f, 0.25f);
            // GUI.DrawTexture(resizeLeft, Texture2D.whiteTexture);
            // GUI.color = new Color(1f, 0f, 0f, 0.25f);
            // GUI.DrawTexture(resizeBottom, Texture2D.whiteTexture);
            //
            // GUI.color = new Color(0f, 1f, 0f, 0.25f);
            // GUI.DrawTexture(resizeBottomRight, Texture2D.whiteTexture);
            // GUI.color = new Color(0f, 1f, 0f, 0.25f);
            // GUI.DrawTexture(resizeBottomLeft, Texture2D.whiteTexture);
            // GUI.color = new Color(0f, 1f, 0f, 0.25f);
            // GUI.DrawTexture(resizeTopLeft, Texture2D.whiteTexture);
            // GUI.color = new Color(0f, 1f, 0f, 0.25f);
            // GUI.DrawTexture(resizeTopRight, Texture2D.whiteTexture);

            GUI.color = Color.white;
        }
    }

    public static class WorkspaceGroupExtensions
    {
        public static IEnumerable<WorkspaceItem> GetContainedItems(this WorkspaceGroup group, Workspace workspace)
            => workspace.Items.Where(x => x.ParentGroup == group);

        public static IEnumerable<WorkspaceItem> GetOverlappingItems(this WorkspaceGroup group, Workspace workspace, Vector2 itemScale)
        {
            return workspace.Items
                .Where(x => x is not WorkspaceGroup)
                .Where(x => group.Rect.Overlaps(x.GetRect(itemScale)));
        }
    }
}