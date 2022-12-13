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
            _input = EditorGUILayout.TextField(_inputLabel, _input);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(_cancelButtonText))
                Cancel();

            if (GUILayout.Button(_submitButtonText))
                Submit();

            GUILayout.EndHorizontal();
        }

        public static TextInputModal GetWindow(string title, string inputLabel, string submitButtonText,
            string cancelButtonText)
        {
            var window = GetWindow<TextInputModal>(true, title);
            window.minSize = window.maxSize = new Vector2(300, EditorGUIUtility.singleLineHeight*3);
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