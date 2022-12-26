namespace Howl.Workspaces
{
    using System;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;

    public class WorkspaceHyperlink : WorkspaceItem
    {
        public override GUIContent ToolbarContent => new(Link.AbsoluteUri, EditorGUIUtility.FindTexture("d_Linked"));

        [JsonProperty] public Uri Link;
        [JsonProperty] public string Name;

        private Texture2D _icon;
        public override void Render(Workspace workspace, Vector2 renderScale, bool selected, GUISkin skin)
        {
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
            GUI.DrawTexture(textureRect, EditorGUIUtility.FindTexture("d_Profiler.UIDetails@2x"));

            if (Locked)
            {
                const int iconSize = 15;
                var rect = new Rect(boxRect.xMax - iconSize*0.75f, boxRect.y - iconSize*0.25f, iconSize, iconSize);
                GUI.DrawTexture(rect, EditorGUIUtility.FindTexture("d_AssemblyLock"), ScaleMode.ScaleToFit);
            }

            string label;
            if (!string.IsNullOrWhiteSpace(Name))
                label = Name;
            else if (selected)
                label = Link?.AbsoluteUri;
            else
                label = Link?.Host;

            var labelContent = new GUIContent(label);
            var labelStyle = new GUIStyle(skin.GetStyle("WorkspaceAsset_Label"));
            if (selected)
            {
                labelStyle.clipping = TextClipping.Overflow;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.normal.textColor = Color.white;
            }
            else
            {
                labelStyle.fontStyle = FontStyle.Normal;
                labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            }

            var size = labelStyle.CalcSize(labelContent);

            var labelRect = new Rect(boxRect);
            labelRect.y = boxRect.yMax;
            labelRect.size = size;

            if (selected)
                GUI.Box(new Rect(labelRect.position, size), GUIContent.none, skin.GetStyle("WorkspaceAsset_LabelFrame"));

            GUI.Label(labelRect, labelContent, labelStyle);
        }

        public override GenericMenu GetGenericMenu(Workspace workspace)
        {
            var menu = new GenericMenu();
            if (Locked)
                menu.AddItem(new GUIContent("Unlock Item"), true, () => workspace.SetItemLock(this, false));
            else
                menu.AddItem(new GUIContent("Lock Item"), false, () => workspace.SetItemLock(this, true));

            menu.AddItem(new GUIContent("Rename..."), false, () => RenamePopup(workspace));
            menu.AddItem(new GUIContent("Remove from workspace"), false, () => workspace.RemoveItem(this));
            menu.AddItem(new GUIContent("Open hyperlink"), false, OnDoubleClick);
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

        private void RenamePopup(Workspace workspace)
        {
            var input = TextInputModal.GetWindow("Rename Hyperlink...", "Name", "Rename", "Cancel");
            input.OnSubmit(s => workspace.RenameItem(this, s));
            input.ShowModalUtility();
        }

        public override void OnDoubleClick()
        {
            System.Diagnostics.Process.Start(Link.ToString());
        }
    }
}