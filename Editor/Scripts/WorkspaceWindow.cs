namespace Howl.Workspaces
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class WorkspaceWindow : EditorWindow
    {
        private const float _backgroundScale = 4f;
        private const string _fileExtension = ".wspace";

        private readonly Vector2 _itemPixelScale = Vector2.one*64;

        private readonly List<WorkspaceItem> _selectedItems = new();
        private Workspace _activeWorkspace;
        private int _activeWorkspaceIndex;

        private Texture2D _backgroundTexture;
        private bool _draggingItems;

        private bool _dragSelecting;
        private Vector2 _dragStartPos;
        private WorkspaceItem _hoveredItem;
        private bool _initialized;
        private static bool _utilityWindow = false;
        private string[] _workspaceNames;
        private string[] _workspacePaths;
        private float _itemScaleFactor = 1;

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
                if (GUI.Button(rect,
                        new GUIContent("New Workspace", EditorGUIUtility.FindTexture("d_CreateAddNew@2x"))))
                {
                    NewWorkspacePopup();
                }
            }
            else
            {
                HandleDragAndDrop();
                HandleSelectedItemDrag();
                DrawItems();
                HandleSelection();
                HandleBoxSelection();

                if (_hoveredItem == null && Event.current.type is EventType.MouseDown && Event.current.button == 1)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Toggle Window Mode"), false, ToggleWindowMode);
                    menu.AddItem(new GUIContent("Sort workspace"), false, SortItems);
                    menu.ShowAsContext();
                }
            }

            DrawToolbar();
            DrawStatusBar();
        }

        private void DrawItems()
        {
            foreach (var item in _activeWorkspace.Items)
                item.Render(_itemScale, _selectedItems.Contains(item));
        }

        private void OnEnable()
        {
            _itemScaleFactor = PlayerPrefs.GetFloat("ItemScaleFactor", _itemScaleFactor);
        }

        private void DrawControls()
        {
            var sliderHeight = 22;
            var sizeSliderRect = new Rect(position.width*0.75f, position.size.y - 20 - sliderHeight, position.width*0.4f, sliderHeight);
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

        private void HandleBoxSelection()
        {
            if (_draggingItems)
                return;

            if (Event.current.type is EventType.MouseDown && _hoveredItem == null)
            {
                _dragStartPos = Event.current.mousePosition;
            }

            if (Event.current.type is EventType.MouseUp)
            {
                _dragSelecting = false;
            }

            if (Event.current.type is EventType.MouseDrag)
            {
                if (Vector2.Distance(Event.current.mousePosition, _dragStartPos) > 10f)
                {
                    _dragSelecting = true;
                }

                if (_dragSelecting)
                {
                    foreach (var item in _activeWorkspace.Items)
                    {
                        if (item.GetRect(_itemScale).Overlaps(_dragSelectRect))
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

        private void SortItems()
        {
            _activeWorkspace.Items.RemoveAll(x => !x.IsValid);
            _activeWorkspace.Items = _activeWorkspace.Items
                .OrderBy(GetItemTypeName)
                .ThenBy(x => x.Name)
                .ToList();

            string GetItemTypeName(WorkspaceItem x)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(x.GetPath());
                return asset.GetType().Name;
            }

            const int marginX = 8;
            const int marginY = 32;
            const int paddingX = 4;
            const int paddingY = 20;

            int columns = Mathf.FloorToInt((position.width - marginX)/(_itemScale.x + paddingX));
            var layoutItems = _activeWorkspace.Items.Where(x => !x.Locked).ToList();
            for (var i = 0; i < layoutItems.Count; i++)
            {
                var item = layoutItems[i];

                var x = marginX + i%columns*(_itemScale.x + paddingX);
                var y = marginY + Mathf.Floor((i - float.Epsilon)/columns)*(_itemScale.y + paddingY);
                item.Position = new Vector2(x, y);
            }

            SaveWorkspace(_activeWorkspace);
        }

        private void HandleSelection()
        {
            bool hovered = false;
            for (var i = _activeWorkspace.Items.Count - 1; i >= 0; i--)
            {
                var item = _activeWorkspace.Items[i];
                if (IsHovered(item))
                {
                    hovered = true;
                    _hoveredItem = item;
                }

                HandleSelection(item);
            }

            if (!hovered)
                _hoveredItem = null;

            if (_hoveredItem == null && !_draggingItems && !_dragSelecting && Event.current.type is EventType.MouseUp)
                _selectedItems.Clear();
        }

        private bool IsHovered(WorkspaceItem item)
        {
            var mousePos = Event.current.mousePosition;
            return item.GetRect(_itemScale).Contains(mousePos);
        }

        [MenuItem("Tools/Workspace")]
        private static void Init()
        {
            var window = GetWindow<WorkspaceWindow>(false, "Workspace");
            window.ShowPopup();
        }

        private void DrawToolbar()
        {
            const int toolbarHeight = 22;
            const int toolbarPadding = 4;
            var rect = new Rect(toolbarPadding, toolbarPadding, position.width - toolbarPadding*2, toolbarHeight);
            GUI.Box(rect, GUIContent.none, "FrameBox");

            // Select workspace
            var popupRect = new Rect(rect);
            popupRect.xMin = rect.xMax - 200;
            popupRect.xMax -= 2;
            popupRect.yMin += 2;
            var index = EditorGUI.Popup(popupRect, _activeWorkspaceIndex, _workspaceNames, "DropDownButton");
            if (index != _activeWorkspaceIndex)
            {
                LoadWorkspace(_workspacePaths[index]);
            }
        }

        private void NewWorkspacePopup()
        {
            var input = TextInputModal.GetWindow("New Workspace", "Workspace Name", "Create Workspace", "Cancel");
            input.OnSubmit(NewWorkspace);
            input.ShowModalUtility();
        }

        private void NewWorkspace(string workspaceName)
        {
            SaveWorkspace(_activeWorkspace);
            var workspace = new Workspace(workspaceName);
            SaveWorkspace(workspace);
            SetActiveWorkspace(workspace);
        }

        private void DrawStatusBar()
        {
            const int statusBarHeight = 22;
            var rect = new Rect(0, position.size.y - statusBarHeight + 2, position.width, statusBarHeight);
            GUI.Box(rect, GUIContent.none, "Toolbar");

            if (_hoveredItem != null)
                GUI.Label(rect, new GUIContent(_hoveredItem.GetPath(), _hoveredItem.GetIcon()));

            var buttonRect = new Rect(rect);
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
        }

        private void DrawBackground()
        {
            var rect = new Rect(Vector2.zero, position.size);
            GUI.DrawTextureWithTexCoords(rect, _backgroundTexture,
                new Rect(Vector2.zero, new Vector2(position.size.x, -position.size.y)*_backgroundScale*0.01f));
        }

        private void Initialize()
        {
            _backgroundTexture = Resources.Load<Texture2D>("Textures/workspace_background");
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

        private void DrawAllItems()
        {
            if (_activeWorkspace.Items == null)
                return;

        }

        private Vector2 _itemScale => _itemScaleFactor*_itemPixelScale;

        private void HandleSelection(WorkspaceItem item)
        {
            var @event = Event.current;
            var eventType = @event.type;
            if (!IsHovered(item))
                return;

            if (eventType is not EventType.MouseDown)
                return;

            // Right-click context menu
            if (@event.button == 1)
            {
                var menu = new GenericMenu();
                if (item.Locked)
                    menu.AddItem(new GUIContent("Unlock Item"), true, () => UnlockItem(item));
                else
                    menu.AddItem(new GUIContent("Lock Item"), false, () => LockItem(item));

                menu.AddItem(new GUIContent("Remove from workspace"), false, () => RemoveItem(item));
                menu.AddItem(new GUIContent("Locate asset"), false,
                    () => EditorGUIUtility.PingObject(item.GetObject()));
                menu.AddItem(new GUIContent("Open asset"), false, () => AssetDatabase.OpenAsset(item.GetObject()));
                menu.AddItem(new GUIContent("Set color/Red"), false, () => SetItemColor(item, Color.red));
                menu.AddItem(new GUIContent("Set color/Orange"), false, () => SetItemColor(item, new Color(1f, 0.5f, 0f)));
                menu.AddItem(new GUIContent("Set color/Yellow"), false, () => SetItemColor(item, Color.yellow));
                menu.AddItem(new GUIContent("Set color/Green"), false, () => SetItemColor(item, Color.green));
                menu.AddItem(new GUIContent("Set color/Blue"), false, () => SetItemColor(item, new Color(0f, 0.5f, 1f)));
                menu.AddItem(new GUIContent("Set color/Pink"), false, () => SetItemColor(item, new Color(1f, 0.5f, 1f)));
                menu.ShowAsContext();
                return;
            }

            // Double-click to open
            if (@event.clickCount == 2)
            {
                AssetDatabase.OpenAsset(item.GetObject());
                return;
            }

            // Ctrl+click to locate
            if (@event.control)
            {
                EditorGUIUtility.PingObject(item.GetObject());
                return;
            }

            if (_selectedItems.Contains(item))
                return;

            if (!@event.shift)
                _selectedItems.Clear();

            if (item.Locked)
                return;

            _selectedItems.Add(item);
            _activeWorkspace.Items.Remove(item);
            _activeWorkspace.Items.Add(item);
            @event.Use();
        }

        private void LockItem(WorkspaceItem item)
        {
            item.Locked = true;
            SaveWorkspace(_activeWorkspace);
        }

        private void UnlockItem(WorkspaceItem item)
        {
            item.Locked = false;
            SaveWorkspace(_activeWorkspace);
        }

        private void SetItemColor(WorkspaceItem item, Color color)
        {
            item.Color = color;
            SaveWorkspace(_activeWorkspace);
        }

        private void PickItemColor(WorkspaceItem item)
        {
            var colorPickerType = Assembly.GetAssembly(typeof(EditorWindow)).GetTypes()
                .FirstOrDefault(x => x.Name == "ColorPicker");

            if (colorPickerType == null)
                return;

            var showColorPicker = colorPickerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "Show" && x.GetParameters()[0].Name == "colorChangedCallback");

            if (showColorPicker == null)
                return;

            showColorPicker.Invoke(null, new object[] { (Action<Color>)SetColor, item.Color, false, false });

            void SetColor(Color color)
            {
                item.Color = color;
                SaveWorkspace(_activeWorkspace);
            }
        }

        private void RemoveItem(WorkspaceItem item)
        {
            _activeWorkspace.Items.Remove(item);
            SaveWorkspace(_activeWorkspace);
        }

        private void HandleSelectedItemDrag()
        {
            var eventType = Event.current.type;
            if (eventType is EventType.MouseDown && _hoveredItem != null)
            {
                _draggingItems = true;
            }

            if (!_draggingItems)
                return;

            if (eventType is EventType.MouseDrag)
            {
                foreach (var selectedItem in _selectedItems.Where(x => !x.Locked))
                {
                    selectedItem.Position += Event.current.delta;
                }
            }
            else if (eventType is EventType.MouseUp)
            {
                _draggingItems = false;
                SaveWorkspace(_activeWorkspace);
            }
        }

        private static float RoundTo(float value, float multipleOf)
            => Mathf.Round(value/multipleOf)*multipleOf;

        /// <summary>
        /// Handles the drag and drop behaviour for adding assets to the workspace
        /// </summary>
        private void HandleDragAndDrop()
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

                    if (_activeWorkspace.Items.Any(x => x.AssetGuid == guid))
                        continue;

                    const int offset = 4;
                    var size = Vector2.one*_itemScale;
                    var pos = Event.current.mousePosition + Vector2.one*i*offset;
                    pos -= size/2f;
                    _activeWorkspace.Items.Add(new WorkspaceItem
                    {
                        Position = pos,
                        AssetGuid = guid
                    });
                }

                if (DragAndDrop.objectReferences.Length > 0)
                {
                    SaveWorkspace(_activeWorkspace);
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

            var json = File.ReadAllText(filePath);
            var workspace = JsonConvert.DeserializeObject<Workspace>(json);
            SetActiveWorkspace(workspace);
        }

        private void SetActiveWorkspace(Workspace workspace)
        {
            ReloadSavedWorkspaces();
            _activeWorkspace = workspace;
            var workspaceIndex = Array.IndexOf(_workspacePaths, GetWorkspaceFilePath(workspace));
            _activeWorkspaceIndex = workspaceIndex;
            titleContent.text = $"Workspace - {workspace.Name}";
        }

        private static string GetWorkspaceFilePath(Workspace workspace)
            => Path.Combine(_workspaceDirectory, GetFileName($"{workspace.Name}{_fileExtension}"));

        private static string GetFileName(string input)
        {
            foreach (char ch in Path.GetInvalidFileNameChars())
                input = input.Replace(ch, '_');

            return input.ToLower();
        }

        private void SaveWorkspace(Workspace workspace)
        {
            if (workspace == null)
                return;

            if (!Directory.Exists(_workspaceDirectory))
                Directory.CreateDirectory(_workspaceDirectory);

            var filePath = GetWorkspaceFilePath(workspace);
            var json = JsonConvert.SerializeObject(workspace);
            File.WriteAllText(filePath, json);
        }
    }
}