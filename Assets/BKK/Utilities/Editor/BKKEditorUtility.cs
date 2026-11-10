using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BKKEditorUtility
{
    public static void DrawLine()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
    
    public static void DrawTitle(string title, GUIStyle style)
    {
        DrawLine();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(title, style);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        DrawLine();
    }
    
    public static Texture2D TextureField(Texture2D texture, float width = 70, float height = 70)
    {
        var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(width), GUILayout.Height(height));
        
        return result;
    }
    
    public static void DrawDragAndDropArea(string text, Action<Object> action)
    {
        EditorGUILayout.Space();
        
        var evt = Event.current;
        var dropArea = GUILayoutUtility.GetRect(100, 50);
        
        GUI.Box(dropArea, text, new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                textColor = Color.green, 
                background = Texture2D.grayTexture
            }, 
            fontSize = 12,
        });
        
        EditorGUILayout.Space();

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition)) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (var objectReference in DragAndDrop.objectReferences)
                    {
                        action?.Invoke(objectReference);
                    }

                    Event.current.Use();
                }
                break;
        }
    }
}
