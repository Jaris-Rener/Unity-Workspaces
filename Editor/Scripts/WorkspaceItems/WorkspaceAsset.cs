namespace Howl.Workspaces
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class WorkspaceAsset : WorkspaceItem
    {
        public override GUIContent ToolbarContent => new(GetPath(), GetIcon());
        public string AssetGuid;

        public string GetPath()
            => AssetDatabase.GUIDToAssetPath(AssetGuid);

        public Object GetObject()
            => AssetDatabase.LoadAssetAtPath<Object>(GetPath());

        public Texture GetIcon()
            => AssetPreview.GetMiniThumbnail(GetObject());

        public string GetName()
            => GetObject().name;

        public bool IsValid => GetObject() != null;
        public override string ToString() => GetName();

        public Type GetObjectType()
            => GetObject().GetType();

        public string GetTypeName()
            => GetObjectType().Name;

        public override GenericMenu GetGenericMenu(Workspace workspace)
        {
            var menu = new GenericMenu();
            if (Locked)
                menu.AddItem(new GUIContent("Unlock Item"), true, () => workspace.SetItemLock(this, false));
            else
                menu.AddItem(new GUIContent("Lock Item"), false, () => workspace.SetItemLock(this, true));

            menu.AddItem(new GUIContent("Remove from workspace"), false, () => workspace.RemoveItem(this));
            menu.AddItem(new GUIContent("Locate asset"), false, () => EditorGUIUtility.PingObject(GetObject()));
            menu.AddItem(new GUIContent("Open asset"), false, () => AssetDatabase.OpenAsset(GetObject()));
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

        public override void OnDoubleClick()
            => AssetDatabase.OpenAsset(GetObject());

        public override void OnCtrlClick()
            => EditorGUIUtility.PingObject(GetObject());

        public override void Render(Workspace workspace, Vector2 renderScale, bool selected, GUISkin skin)
        {
            if (!IsValid)
                return;

            var boxRect = new Rect(Position, renderScale);
            if (selected)
                GUI.Box(boxRect, GUIContent.none, skin.GetStyle("WorkspaceAsset_SelectionOutline"));

            GUI.color = Color;
            GUI.Box(boxRect, GUIContent.none, skin.GetStyle("WorkspaceAsset"));
            GUI.color = Color.white;

            var padding = new Vector2(8, 8);
            var textureRect = new Rect(boxRect);
            textureRect.size -= padding*2;
            textureRect.position += padding;
            GUI.DrawTexture(textureRect, GetIcon());

            if (Locked)
            {
                const int iconSize = 15;
                var rect = new Rect(boxRect.xMax - iconSize*0.75f, boxRect.y - iconSize*0.25f, iconSize, iconSize);
                GUI.DrawTexture(rect, EditorGUIUtility.FindTexture("d_AssemblyLock"), ScaleMode.ScaleToFit);
            }

            var labelRect = new Rect(boxRect);
            labelRect.height = EditorGUIUtility.singleLineHeight;
            labelRect.y = boxRect.yMax;
            var labelStyle = new GUIStyle(skin.GetStyle("WorkspaceAsset_Label"));
            if (selected)
            {
                labelStyle.clipping = TextClipping.Overflow;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.normal.textColor = Color.white;

                var size = labelStyle.CalcSize(new GUIContent(GetName()));
                GUI.Box(new Rect(labelRect.position, size), GUIContent.none, skin.GetStyle("WorkspaceAsset_LabelFrame"));
            }
            else
            {
                labelStyle.fontStyle = FontStyle.Normal;
                labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            }

            GUI.Label(labelRect, GetName(), labelStyle);
        }
    }
}