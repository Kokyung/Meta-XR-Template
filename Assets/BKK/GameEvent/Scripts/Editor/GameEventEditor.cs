using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using BKK.Extension;
using BKK.Extension.Editor;

namespace BKK.GameEventArchitecture.Editor
{
    [CustomEditor(typeof(GameEvent))]
    public class GameEventEditor : UnityEditor.Editor
    {
        private GameEvent gameEvent;

        private Vector2 scroll;

        private bool locked;

        private GUIStyle descriptionStyle;

        private void OnEnable()
        {
            gameEvent = (GameEvent) target;
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isPlaying)
            {
                EditorPrefs.SetBool(GameEventGlobalOption.locked, true);
            }

            EditorGUILayout.LabelField("[ Debug ]", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying) GUI.enabled = false;
            
            if (GUILayout.Button("Raise"))
            {
                gameEvent.Raise();
            }

            if (GUILayout.Button("Cancel"))
            {
                gameEvent.Cancel();
            }

            if (!EditorApplication.isPlaying) GUI.enabled = true;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("[ Description ]", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll,
                GUILayout.MaxHeight(EditorPrefs.GetInt(GameEventGlobalOption.typingAreaHeight) *
                                    EditorGUIUtility.singleLineHeight));
            
            if (EditorPrefs.GetBool(GameEventGlobalOption.locked)) GUI.enabled = false;

            EditorGUI.BeginChangeCheck();

            var description = WithoutSelectAll(() => EditorGUILayout.TextArea(gameEvent.description,
                GetDescriptionStyle(),
                GUILayout.MaxHeight(EditorPrefs.GetInt(GameEventGlobalOption.typingAreaHeight) *
                                    EditorGUIUtility.singleLineHeight)));

            // 값이 바뀐 경우에만 변경 전 상태를 Undo에 기록하고 반영한다.
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gameEvent, "Game Event Description");
                gameEvent.description = description;
                EditorUtility.SetDirty(gameEvent);
            }

            if (EditorPrefs.GetBool(GameEventGlobalOption.locked)) GUI.enabled = true;

            EditorGUILayout.EndScrollView();

            var fontSizeValue = EditorGUILayout.IntField("Font Size", EditorPrefs.GetInt(GameEventGlobalOption.fontSize));
            if (fontSizeValue < 1) fontSizeValue = 1;
            EditorPrefs.SetInt(GameEventGlobalOption.fontSize, fontSizeValue);
            
            var typingAreaHeightValue = EditorGUILayout.IntField("Input Field Height", EditorPrefs.GetInt(GameEventGlobalOption.typingAreaHeight));
            if (typingAreaHeightValue < 1) typingAreaHeightValue = 1;
            EditorPrefs.SetInt(GameEventGlobalOption.typingAreaHeight, typingAreaHeightValue);
            
            if (EditorApplication.isPlaying) GUI.enabled = false;
            if (EditorPrefs.GetBool(GameEventGlobalOption.locked))
            {
                if (GUILayout.Button("Edit"))
                {
                    EditorPrefs.SetBool(GameEventGlobalOption.locked, false);
                }
            }
            else
            {
                if (GUILayout.Button("Save"))
                {
                    EditorPrefs.SetBool(GameEventGlobalOption.locked, true);
                    EditorUtility.SetDirty(gameEvent);
                }
            }


            if (EditorApplication.isPlaying) GUI.enabled = true;
        }

        // EditorStyles.textArea를 직접 바꾸면 에디터 전체 TextArea에 적용되므로 복사본을 사용한다.
        private GUIStyle GetDescriptionStyle()
        {
            descriptionStyle ??= new GUIStyle(EditorStyles.textArea);
            descriptionStyle.fontSize = EditorPrefs.GetInt(GameEventGlobalOption.fontSize);
            return descriptionStyle;
        }

        /// <summary>
        /// 텍스트 에이리어 선택시 텍스트가 전부 선택되는 문제를 방지한다.
        /// </summary>
        /// <param name="guiCall">레이아웃 메서드</param>
        /// <typeparam name="T">리턴 타입</typeparam>
        /// <returns></returns>
        private T WithoutSelectAll<T>(System.Func<T> guiCall)
        {
            bool preventSelection = (Event.current.type == EventType.MouseDown);

            Color oldCursorColor = GUI.skin.settings.cursorColor;

            if (preventSelection)
                GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);

            T value = guiCall();

            if (preventSelection)
                GUI.skin.settings.cursorColor = oldCursorColor;

            return value;
        }
    }
    
    [CustomEditor(typeof(GameEvent<>), true)]
    public class CustomTypeGameEventEditor : UnityEditor.Editor
    {
        private Vector2 scroll;
    
        private bool locked;

        private GUIStyle descriptionStyle;

        private SerializedProperty _debugValue;
        private SerializedProperty _description;
        private MethodInfo _raiseMethod;
        private MethodInfo _cancelMethod;
        
        private readonly string debugValuePropertyName = "debugValue";
        private readonly string descriptionPropertyName = "description";

        private GUIContent debugValueContent = new GUIContent("Deug Value");
        
        private void OnEnable()
        {
            _debugValue = serializedObject.FindProperty(debugValuePropertyName);
            _description = serializedObject.FindProperty(descriptionPropertyName);
            // 상속 깊이와 상관없이 최종 오버라이드된 메서드를 찾는다.
            _raiseMethod = target.GetType().GetMethod("Raise", BindingFlags.Instance | BindingFlags.Public);
            _cancelMethod = target.GetType().GetMethod("Cancel", BindingFlags.Instance | BindingFlags.Public);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (EditorApplication.isPlaying) EditorPrefs.SetBool(GameEventGlobalOption.locked, true);

            EditorGUILayout.LabelField("[ Debug ]", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying) GUI.enabled = false;

            EditorGUILayout.PropertyField(_debugValue, debugValueContent, true);

            if (GUILayout.Button("Raise"))
            {
                // debug 값은 어떤 타입의 값이든 될 수 있기 때문에 System.Reflection 기능을 활용하여 가져온다.
                object debugValue = EditorExtension.GetFieldValue(_debugValue, debugValuePropertyName);
                MethodExtension.ExecuteMethod(target, _raiseMethod, debugValue);
            }

            if (GUILayout.Button("Cancel"))
            {
                object debugValue = EditorExtension.GetFieldValue(_debugValue, debugValuePropertyName);
                MethodExtension.ExecuteMethod(target, _cancelMethod, debugValue);
            }

            if (!EditorApplication.isPlaying) GUI.enabled = true;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("[ Description ]", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll,
                GUILayout.MaxHeight(EditorPrefs.GetInt(GameEventGlobalOption.typingAreaHeight) * EditorGUIUtility.singleLineHeight));

            if (EditorPrefs.GetBool(GameEventGlobalOption.locked)) GUI.enabled = false;

            EditorGUI.BeginChangeCheck();

            var description = WithoutSelectAll(() => EditorGUILayout.TextArea(_description.stringValue,
                GetDescriptionStyle(),
                GUILayout.MaxHeight(EditorPrefs.GetInt(GameEventGlobalOption.typingAreaHeight) * EditorGUIUtility.singleLineHeight)));

            // 값이 바뀐 경우에만 반영한다. Undo 기록과 Dirty 처리는 ApplyModifiedProperties가 담당한다.
            if (EditorGUI.EndChangeCheck()) _description.stringValue = description;

            if (EditorPrefs.GetBool(GameEventGlobalOption.locked)) GUI.enabled = true;

            EditorGUILayout.EndScrollView();
            
            var fontSizeValue = EditorGUILayout.IntField("Font Size", EditorPrefs.GetInt(GameEventGlobalOption.fontSize));
            if (fontSizeValue < 1) fontSizeValue = 1;
            EditorPrefs.SetInt(GameEventGlobalOption.fontSize, fontSizeValue);
            
            var typingAreaHeightValue = EditorGUILayout.IntField("Input Field Height", EditorPrefs.GetInt(GameEventGlobalOption.typingAreaHeight));
            if (typingAreaHeightValue < 1) typingAreaHeightValue = 1;
            EditorPrefs.SetInt(GameEventGlobalOption.typingAreaHeight, typingAreaHeightValue);

            if (EditorApplication.isPlaying) GUI.enabled = false;
            if (EditorPrefs.GetBool(GameEventGlobalOption.locked))
            {
                if (GUILayout.Button("Edit"))
                {
                    EditorPrefs.SetBool(GameEventGlobalOption.locked, false);
                }
            }
            else
            {
                if (GUILayout.Button("Save"))
                {
                    EditorPrefs.SetBool(GameEventGlobalOption.locked, true);
                    EditorUtility.SetDirty(target);
                }
            }
            
            if (EditorApplication.isPlaying) GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        // EditorStyles.textArea를 직접 바꾸면 에디터 전체 TextArea에 적용되므로 복사본을 사용한다.
        private GUIStyle GetDescriptionStyle()
        {
            descriptionStyle ??= new GUIStyle(EditorStyles.textArea);
            descriptionStyle.fontSize = EditorPrefs.GetInt(GameEventGlobalOption.fontSize);
            return descriptionStyle;
        }

        /// <summary>
        /// 텍스트 에이리어 선택시 텍스트가 전부 선택되는 문제를 방지한다.
        /// </summary>
        /// <param name="guiCall">레이아웃 메서드</param>
        /// <typeparam name="T">리턴 타입</typeparam>
        /// <returns></returns>
        private T WithoutSelectAll<T>(System.Func<T> guiCall)
        {
            bool preventSelection = (Event.current.type == EventType.MouseDown);
    
            Color oldCursorColor = GUI.skin.settings.cursorColor;
    
            if (preventSelection)
                GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
    
            T value = guiCall();
    
            if (preventSelection)
                GUI.skin.settings.cursorColor = oldCursorColor;
    
            return value;
        }
    }
}

