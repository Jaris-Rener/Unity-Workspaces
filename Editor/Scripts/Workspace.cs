namespace Howl.Workspaces
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class Workspace
    {
        public List<WorkspaceItem> Items = new();
        public string Name;

        public Workspace(string name)
        {
            Name = name;
        }
    }
}