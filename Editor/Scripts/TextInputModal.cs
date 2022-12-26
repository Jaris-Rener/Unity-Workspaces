namespace Howl.Workspaces
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public class TextInputModal : EditorWindow
    {
        [SerializeField] private string _inputLabel;
        [SerializeField] private string _submitButtonText;
        [SerializeField] private string _cancelButtonText;
        [SerializeField] private string _input;

        private Action _onCancelCallback;
        private Action<string> _onSubmitCallback;

        private void OnGUI()
        {
            const int padding = 12;
            GUILayout.BeginArea(new Rect(padding, padding,
                position.width - padding*2, position.height - padding*2));
            GUILayout.BeginHorizontal();
            GUI.color = Color.black;
            GUILayout.Label(GUIContent.none, "GroupBox", GUILayout.Width(64), GUILayout.Height(64));
            GUI.color = Color.white;
            GUI.Label(GUILayoutUtility.GetLastRect().AddPadding(6), EditorGUIUtility.IconContent("d_UnityLogo"));
            GUILayout.Space(12);
            GUILayout.BeginVertical();
            GUILayout.Label(_inputLabel);
            GUILayout.FlexibleSpace();
            _input = EditorGUILayout.TextField(_input);
            GUILayout.Space(2);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_cancelButtonText, GUILayout.Width(100)))
                Cancel();

            if (GUILayout.Button(_submitButtonText, GUILayout.Width(100)))
                Submit();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (Event.current.type is EventType.KeyUp)
            {
                if (Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                {
                    Submit();
                }
            }
        }

        public static TextInputModal GetWindow(string title, string inputLabel, string submitButtonText,
            string cancelButtonText)
        {
            var window = GetWindow<TextInputModal>(true, title);
            window.minSize = window.maxSize = new Vector2(350, 130);
            window._inputLabel = inputLabel;
            window._submitButtonText = submitButtonText;
            window._cancelButtonText = cancelButtonText;
            return window;
        }

        private void Cancel()
        {
            _onCancelCallback?.Invoke();
            Close();
        }

        private void Submit()
        {
            _onSubmitCallback?.Invoke(_input);
            Close();
        }

        public void OnCancel(Action onCancelCallback)
        {
            _onCancelCallback = onCancelCallback;
        }

        public void OnSubmit(Action<string> onSubmitCallback)
        {
            _onSubmitCallback = onSubmitCallback;
        }
    }
}