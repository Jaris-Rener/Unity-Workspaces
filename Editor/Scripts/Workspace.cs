namespace Howl.Workspaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using UnityEngine;

    [Serializable]
    public class Workspace
    {
        [JsonIgnore] public bool IsDirty { get; set; }

        public void SetDirty() => IsDirty = true;

        public List<WorkspaceItem> Items = new();
        public string Name;

        public Workspace(string name)
        {
            Name = name;
        }
    }

    public static class WorkspaceExtensions
    {
        public static IEnumerable<WorkspaceAsset> GetWorkspaceAssets(this Workspace workspace)
            => workspace.Items.OfType<WorkspaceAsset>();

        public static bool ContainsAsset(this Workspace workspace, string assetGuid)
            => workspace.GetWorkspaceAssets().Any(x => x.AssetGuid == assetGuid);

        public static void SetItemLock(this Workspace workspace, WorkspaceItem item, bool locked)
        {
            item.Locked = locked;
            workspace.SetDirty();
        }

        public static void RemoveItem(this Workspace workspace, WorkspaceItem item)
        {
            workspace.Items.Remove(item);
            workspace.SetDirty();
        }

        public static void SetItemColor(this Workspace workspace, WorkspaceItem item, Color color)
        {
            item.Color = color;
            workspace.SetDirty();
        }

        public static void LayoutItems(this Workspace workspace, Rect layoutArea, Vector2 itemScale, params WorkspaceItem[] items)
        {
            const int paddingX = 4;
            const int paddingY = 20;

            int columns = Mathf.FloorToInt(layoutArea.width/(itemScale.x + paddingX));
            for (var i = 0; i < items.Length; i++)
            {
                var x = layoutArea.x + i%columns*(itemScale.x + paddingX);
                var y = layoutArea.y + Mathf.Floor((i - float.Epsilon)/columns)*(itemScale.y + paddingY);
                items[i].Position = new Vector2(x, y);
            }

            workspace.SetDirty();
        }
    }
}