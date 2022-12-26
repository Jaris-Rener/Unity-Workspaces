namespace Howl.Workspaces
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using Random = UnityEngine.Random;

    public struct WorkspaceNotification
    {
        public double CreationTime;
        public string Message;
    }

    public class WorkspaceWindow : EditorWindow
    {
        private const float _backgroundScale = 4f;
        private const string _fileExtension = ".wspace";

        private static readonly Vector2 _itemPixelScale = Vector2.one*64;

        private readonly List<WorkspaceItem> _selectedItems = new();
        private Workspace _activeWorkspace;
        private int _activeWorkspaceIndex;

        private Texture2D _backgroundTexture;
        private bool _draggingItems;

        private bool _dragSelecting;
        private Vector2 _dragStartPos;
        private WorkspaceItem _hoveredItem;
        private bool _initialized;
        private static bool _utilityWindow;
        private string[] _workspaceNames;
        private string[] _workspacePaths;
        private static float _itemScaleFactor = 1;

        private Rect _safeZone => new(8, 8, position.width - 16, position.height - _toolbarRect.height - 16);
        private Rect _notificationArea => new (_safeZone);

        private static string _workspaceDirectory
            => Path.Combine(Application.persistentDataPath, "Workspaces").Replace('/', '\\');

        private Rect _dragSelectRect => _dragSelecting
            ? new Rect(_dragStartPos, Event.current.mousePosition - _dragStartPos).GetNonInvertedRect()
            : Rect.zero;

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (!_initialized)
                Initialize();

            GUI.color = _activeWorkspace == null ? Color.grey : Color.white;
            DrawBackground();
            GUI.color = Color.white;

            var val = Mathf.Clamp01((Event.current.mousePosition.y - position.height + 75)/24);
            GUI.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.white, val);
            DrawControls();
            GUI.color = Color.white;

            if (_activeWorkspace == null)
            {
                var width = 192;
                var height = 64;
                var rect = new Rect(position.width/2f - width/2f, position.height/2f - height/2f, width, height);
                if (GUI.Button(rect, new GUIContent("New Workspace", EditorGUIUtility.FindTexture("d_CreateAddNew@2x"))))
                {
                    NewWorkspacePopup();
                }

                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ArrowPlus);
                return;
            }

            _activeWorkspace.Items ??= new();
            HandleDragAndDropAssets();
            HandleSelectedItemDrag();
            DrawItems();
            HandleSelection();
            HandleBoxSelection();

            if (_hoveredItem == null && Event.current.type is EventType.MouseDown && Event.current.button == 1)
            {
                var menu = new GenericMenu();
                var pos = Event.current.mousePosition;
                menu.AddItem(new GUIContent("Add asset..."), false, () => AddAssetPopup(pos));
                menu.AddItem(new GUIContent("Add hyperlink..."), false, () => AddLinkPopup(pos));
                menu.AddItem(new GUIContent("Toggle Window Mode"), false, ToggleWindowMode);
                menu.AddItem(new GUIContent("Sort workspace"), false, SortUngroupedItems);
                menu.ShowAsContext();
            }

            DrawToolbar();
            DrawNotifications();

            HandleObjectPicker();
            HandleHotkeys();
        }

        private void AddLinkPopup(Vector2 pos)
        {
            var input = TextInputModal.GetWindow("Add Hyperlink", "URL", "Add", "Cancel");
            input.OnSubmit(s => AddLink(s, pos));
            input.ShowModalUtility();
        }

        private void AddLink(string url, Vector2 pos)
        {
            var link = new WorkspaceHyperlink
            {
                Link = new Uri(url),
                Position = pos
            };

            _activeWorkspace.Items.Add(link);
        }

        private readonly List<WorkspaceNotification> _notifications = new();
        private const float _notificationDuration = 3f;
        private const float _fadeTime = 0.35f;
        private void DrawNotifications()
        {
            GUILayout.BeginArea(_notificationArea);
            GUILayout.FlexibleSpace();
            for (var i = _notifications.Count - 1; i >= 0; i--)
            {
                var notification = _notifications[i];
                var lifetime = EditorApplication.timeSinceStartup - notification.CreationTime;
                if (lifetime > _notificationDuration - _fadeTime)
                {
                    var x = lifetime - _notificationDuration + _fadeTime;
                    var f = (float)(x/_fadeTime); // 0 - 1 linear
                    GUI.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), f);
                }

                GUILayout.Label(notification.Message, _skin.GetStyle("Notification"));
                GUI.color = Color.white;

                if (lifetime > _notificationDuration)
                    _notifications.Remove(notification);
            }

            GUILayout.EndArea();
        }

        public void PushNotification(string message)
        {
            var notification = new WorkspaceNotification();
            notification.Message = message;
            notification.CreationTime = EditorApplication.timeSinceStartup;
            _notifications.Add(notification);
        }

        private void HandleObjectPicker()
        {
            if (Event.current.commandName == "ObjectSelectorClosed" &&
                EditorGUIUtility.GetObjectPickerControlID() == _curObjectPickerWindow)
            {
                var obj = EditorGUIUtility.GetObjectPickerObject();
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _))
                    return;

                if (_activeWorkspace.ContainsAsset(guid))
                    return;

                var size = Vector2.one*ItemScale;
                _addPosition -= size/2f;
                _activeWorkspace.Items.Add(new WorkspaceAsset
                {
                    Position = _addPosition,
                    AssetGuid = guid
                });
            }
        }

        private int _curObjectPickerWindow;
        private Vector2 _addPosition;

        private void AddAssetPopup(Vector2 pos)
        {
            _addPosition = pos;
            _curObjectPickerWindow = GUIUtility.GetControlID(FocusType.Passive) + 100;
            EditorGUIUtility.ShowObjectPicker<Object>(null, false, string.Empty, _curObjectPickerWindow);
        }

        private void HandleHotkeys()
        {
            if (Event.current.type is EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Delete:
                        RemoveItems(_selectedItems);
                        _selectedItems.Clear();
                        break;
                    case KeyCode.C:
                        _activeWorkspace.Items.Add(new WorkspaceGroup
                        {
                            Position = Event.current.mousePosition,
                            Color = Color.HSVToRGB(Random.value, 1, 1)
                        });
                        break;
                    case KeyCode.S when Event.current.control:
                        SaveWorkspace(_activeWorkspace);
                        break;
                }
            }
        }

        private void RemoveItems(List<WorkspaceItem> items)
        {
            foreach (var item in items)
            {
                _activeWorkspace.Items.Remove(item);
            }

            _activeWorkspace.SetDirty();
        }

        private void DrawItems()
        {
            if (_activeWorkspace.Items == null)
                return;

            foreach (var item in _activeWorkspace.Items.OrderBy(x => x.RenderLayer))
                item.Render(_activeWorkspace, ItemScale, _selectedItems.Contains(item), _skin);
        }

        private void OnEnable()
        {
            _itemScaleFactor = PlayerPrefs.GetFloat("ItemScaleFactor", _itemScaleFactor);
        }

        private void DrawControls()
        {
            var sliderHeight = 22;
            var sizeSliderRect = new Rect(position.width - 128, position.size.y - 20 - sliderHeight, 0, sliderHeight);
            sizeSliderRect.xMax = position.width - 8;
            var itemScale = GUI.HorizontalSlider(sizeSliderRect, _itemScaleFactor, 0.5f, 1.75f);
            if (Math.Abs(_itemScaleFactor - itemScale) > 0.0001f)
            {
                PlayerPrefs.SetFloat("ItemScaleFactor", itemScale);
                _itemScaleFactor = itemScale;
            }
        }

        private void ToggleWindowMode()
        {
            Close();
            _utilityWindow = !_utilityWindow;
            var window = GetWindow<WorkspaceWindow>(_utilityWindow, "Workspace");
            if (_utilityWindow)
                window.ShowUtility();
            else
                window.ShowTab();
        }

        private bool _dragStarted;
        private void HandleBoxSelection()
        {
            if (_draggingItems)
                return;

            if (Event.current.type is EventType.MouseDown && _hoveredItem == null)
            {
                _dragStarted = true;
                _dragStartPos = Event.current.mousePosition;
            }

            if (Event.current.type is EventType.MouseUp)
            {
                _dragSelecting = false;
                _dragStarted = false;
            }

            if (_dragStarted && Event.current.type is EventType.MouseDrag)
            {
                if (Vector2.Distance(Event.current.mousePosition, _dragStartPos) > 10f)
                {
                    _dragSelecting = true;
                }

                if (_dragSelecting)
                {
                    foreach (var item in _activeWorkspace.Items)
                    {
                        if (item.GetRect(ItemScale).Overlaps(_dragSelectRect))
                        {
                            if (item.Locked)
                                continue;

                            if (!_selectedItems.Contains(item))
                                _selectedItems.Add(item);
                        }
                        else
                        {
                            if (_selectedItems.Contains(item))
                                _selectedItems.Remove(item);
                        }
                    }
                }
            }

            if (_dragSelecting)
                GUI.Box(_dragSelectRect, GUIContent.none, "SelectionRect");
        }

        private void SortUngroupedItems()
        {
            var items = _activeWorkspace.GetWorkspaceAssets()
                .Where(x => x.IsValid && !x.Locked)
                .Where(x => x.ParentGroup == null)
                .OrderBy(x => x.GetTypeName())
                .ThenBy(x => x.GetName())
                .ToArray();

            _activeWorkspace.LayoutItems(_safeZone, ItemScale, items);
            PushNotification("Sorted workspace items");
        }

        private void HandleSelection()
        {
            bool hovered = false;
            foreach (var item in _activeWorkspace.Items.OrderBy(x => x.RenderLayer))
            {
                if (!IsHovered(item))
                    continue;

                hovered = true;
                _hoveredItem = item;
            }

            if (_hoveredItem != null)
                HandleSelection(_hoveredItem);

            if (!hovered)
                _hoveredItem = null;

            if (_hoveredItem == null && !_draggingItems && !_dragSelecting && Event.current.type is EventType.MouseUp)
                _selectedItems.Clear();
        }

        private bool IsHovered(WorkspaceItem item)
        {
            var mousePos = Event.current.mousePosition;
            return item.GetRect(ItemScale).Contains(mousePos);
        }

        [MenuItem("Tools/Workspace ^#w")]
        private static void Init()
        {
            var window = GetWindow<WorkspaceWindow>(false, "Workspace");
            window.ShowPopup();
        }

        private void NewWorkspacePopup()
        {
            var input = TextInputModal.GetWindow("New Workspace", "Workspace Name", "Create Workspace", "Cancel");
            input.OnSubmit(NewWorkspace);
            input.ShowModalUtility();
        }

        private void NewWorkspace(string workspaceName)
        {
            if (_activeWorkspace.IsDirty)
            {
                var dialogResult = EditorUtility.DisplayDialogComplex("Save Current Workspace", "Do you want to save the changes you made before switching workspace?", "Save", "Cancel", "No");
                switch (dialogResult)
                {
                    case 0:
                        SaveWorkspace(_activeWorkspace);
                        break;
                    case 1:
                        return;
                    case 2:
                        break;
                }
            }

            var workspace = new Workspace(workspaceName);
            SaveWorkspace(workspace);
            SetActiveWorkspace(workspace);
        }

        private const int _toolbarHeight = 22;
        private Rect _toolbarRect => new(0, position.size.y - _toolbarHeight + 2, position.width, _toolbarHeight);
        private void DrawToolbar()
        {
            GUI.Box(_toolbarRect, GUIContent.none, "Toolbar");

            if (_hoveredItem != null)
                GUI.Label(_toolbarRect, _hoveredItem.ToolbarContent);

            var buttonRect = new Rect(_toolbarRect);
            var buttonWidth = 24;
            buttonRect.xMin = buttonRect.xMax - buttonWidth;
            buttonRect.width = buttonWidth;
            if (GUI.Button(buttonRect, new GUIContent(EditorGUIUtility.FindTexture("d_Folder Icon"), "Open In Explorer"), "toolbarbutton"))
            {
                System.Diagnostics.Process.Start("explorer.exe", _workspaceDirectory);
            }

            buttonRect.x -= buttonWidth;
            if (GUI.Button(buttonRect, new GUIContent(EditorGUIUtility.FindTexture("d_Toolbar Plus"), "New Workspace"), "toolbarbutton"))
            {
                NewWorkspacePopup();
            }

            EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.ArrowPlus);

            var popupWidth = 200;
            var popupRect = new Rect(buttonRect);
            popupRect.width = popupWidth;
            popupRect.x -= popupWidth;
            GUI.contentColor = _activeWorkspace.IsDirty ? new Color(1f, 0.75f, 0f) : Color.white;
            var names = _workspaceNames.ToArray();
            if(_activeWorkspace.IsDirty)
                names[_activeWorkspaceIndex] = $"{names[_activeWorkspaceIndex]} *";

            var index = EditorGUI.Popup(popupRect, _activeWorkspaceIndex, names, "ToolbarPopup");
            GUI.contentColor = Color.white;
            if (index != _activeWorkspaceIndex)
            {
                LoadWorkspace(_workspacePaths[index]);
            }

            var refreshRect = new Rect(popupRect);
            refreshRect.width = buttonWidth;
            refreshRect.x -= buttonWidth;
            if (GUI.Button(refreshRect, new GUIContent(EditorGUIUtility.FindTexture("d_Refresh"), "Reload Workspace"), "toolbarbutton"))
            {
                LoadWorkspace(GetWorkspaceFilePath(_activeWorkspace));
            }

            var sortRect = new Rect(refreshRect);
            sortRect.width = buttonWidth;
            sortRect.x -= buttonWidth;
            if (GUI.Button(sortRect, new GUIContent(EditorGUIUtility.FindTexture("d_GridLayoutGroup Icon"), "Sort Workspace"),
                    "toolbarbutton"))
            {
                SortUngroupedItems();
            }

        }

        private void DrawBackground()
        {
            var rect = new Rect(Vector2.zero, position.size);
            GUI.DrawTextureWithTexCoords(rect, _backgroundTexture,
                new Rect(Vector2.zero, new Vector2(position.size.x, -position.size.y)*_backgroundScale*0.01f));
        }

        private GUISkin _skin;
        private readonly JsonSerializerSettings _jsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        private void Initialize()
        {
            _skin = Resources.Load<GUISkin>("Miscellaneous/WorkspaceGUISkin");
            _backgroundTexture = Resources.Load<Texture2D>("Textures/WorkspaceBackground");
            ReloadSavedWorkspaces();
            if (_workspacePaths.Length > 0)
                LoadWorkspace(_workspacePaths[0]);

            _initialized = true;
        }

        private void ReloadSavedWorkspaces()
        {
            if (Directory.Exists(_workspaceDirectory))
            {
                _workspacePaths = Directory.GetFiles(_workspaceDirectory, $"*{_fileExtension}");
                _workspaceNames = _workspacePaths.Select(Path.GetFileNameWithoutExtension).ToArray();
            }
            else
            {
                _workspacePaths = Array.Empty<string>();
                _workspaceNames = Array.Empty<string>();
            }
        }

        public static Vector2 ItemScale => _itemScaleFactor*_itemPixelScale;

        private void HandleSelection(WorkspaceItem item)
        {
            var @event = Event.current;
            var eventType = @event.type;
            if (!IsHovered(item))
                return;

            if (eventType is EventType.MouseDown)
            {
                if (@event.button == 1)
                {
                    var menu = item.GetGenericMenu(_activeWorkspace);
                    menu.ShowAsContext();
                    return;
                }

                // Double-click to open
                if (@event.clickCount == 2)
                {
                    item.OnDoubleClick();
                    return;
                }

                if (@event.control)
                {
                    item.OnCtrlClick();
                    return;
                }

                if (_selectedItems.Contains(item))
                {
                    if (@event.shift)
                        _selectedItems.Remove(item);

                    return;
                }

                if (!@event.shift)
                    _selectedItems.Clear();

                _selectedItems.Add(item);

                // Bring to front
                _activeWorkspace.Items.Remove(item);
                _activeWorkspace.Items.Add(item);

                @event.Use();
            }
        }

        private void HandleSelectedItemDrag()
        {
            var eventType = Event.current.type;
            if (eventType is EventType.MouseDown && _hoveredItem != null)
            {
                if (_hoveredItem.GetDragRect(ItemScale).Contains(Event.current.mousePosition))
                {
                    _draggingItems = true;
                }
            }

            if (!_draggingItems)
                return;

            if (eventType is EventType.MouseDrag)
            {
                var dragItems = _selectedItems.Where(x => !x.Locked);
                foreach (var dragItem in dragItems)
                {
                    if (dragItem is WorkspaceGroup group)
                        foreach (var item in _activeWorkspace.Items.Where(x => x.ParentGroup == group))
                            item.Position += Event.current.delta;

                    dragItem.Position += Event.current.delta;
                }
            }
            else if (eventType is EventType.MouseUp)
            {
                if (_selectedItems.Count <= 0)
                {
                    _draggingItems = false;
                    return;
                }

                var workspaceGroups = _activeWorkspace.Items.OfType<WorkspaceGroup>().ToList();
                foreach (var item in _selectedItems)
                {
                    SetParentGroup(item);
                }

                void SetParentGroup(WorkspaceItem item)
                {
                    foreach (var group in workspaceGroups)
                    {
                        if (!group.Rect.Contains(item.GetRect(ItemScale).center))
                            continue;

                        item.ParentGroup = group;
                        return;
                    }

                    item.ParentGroup = null;
                }

                _draggingItems = false;
                _activeWorkspace.SetDirty();
            }
        }

        /// <summary>
        /// Handles the drag and drop behaviour for adding assets to the workspace
        /// </summary>
        private void HandleDragAndDropAssets()
        {
            var eventType = Event.current.type;
            if (eventType is not (EventType.DragUpdated or EventType.DragPerform))
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            if (eventType == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    var item = DragAndDrop.objectReferences[i];
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item, out var guid, out long _))
                        continue;

                    if (_activeWorkspace.ContainsAsset(guid))
                        continue;

                    const int offset = 4;
                    var size = Vector2.one*ItemScale;
                    var pos = Event.current.mousePosition + Vector2.one*i*offset;
                    pos -= size/2f;
                    _activeWorkspace.Items.Add(new WorkspaceAsset
                    {
                        Position = pos,
                        AssetGuid = guid
                    });
                }

                if (DragAndDrop.objectReferences.Length > 0)
                {
                    _activeWorkspace.SetDirty();
                }
            }

            Event.current.Use();
        }

        private void LoadWorkspace(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            if (!File.Exists(filePath))
            {
                ReloadSavedWorkspaces();
                return;
            }

            if (_activeWorkspace is { IsDirty: true })
            {
                var dialogResult = EditorUtility.DisplayDialogComplex("Save Current Workspace", "Do you want to save the changes you made before switching workspace?", "Save", "Cancel", "No");
                switch (dialogResult)
                {
                    case 0:
                        SaveWorkspace(_activeWorkspace);
                        break;
                    case 1:
                        return;
                    case 2:
                        break;
                }
            }

            var json = File.ReadAllText(filePath);
            var workspace = JsonConvert.DeserializeObject<Workspace>(json, _jsonSerializerSettings);
            SetActiveWorkspace(workspace);
            PushNotification($"Opened workspace: {workspace.Name}");
        }

        private void SetActiveWorkspace(Workspace workspace)
        {
            ReloadSavedWorkspaces();
            _activeWorkspace = workspace;
            var workspaceIndex = Array.IndexOf(_workspacePaths, GetWorkspaceFilePath(workspace));
            _activeWorkspaceIndex = workspaceIndex;
            titleContent.text = $"{workspace.Name} (Workspace)";
        }

        private static string GetWorkspaceFilePath(Workspace workspace)
            => Path.Combine(_workspaceDirectory, GetFileName($"{workspace.Name}{_fileExtension}"));

        private static string GetFileName(string input)
        {
            foreach (char ch in Path.GetInvalidFileNameChars())
                input = input.Replace(ch, '_');

            return input;
        }

        private void SaveWorkspace(Workspace workspace)
        {
            if (workspace == null)
                return;

            if (!Directory.Exists(_workspaceDirectory))
                Directory.CreateDirectory(_workspaceDirectory);

            var filePath = GetWorkspaceFilePath(workspace);
            var json = JsonConvert.SerializeObject(workspace, _jsonSerializerSettings);
            File.WriteAllText(filePath, json);
            workspace.IsDirty = false;
            PushNotification($"Saved workspace: {workspace.Name}");
        }
    }
}