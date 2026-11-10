using UnityEngine;
using UnityEditor;

/// <summary>
/// 커스텀 클래스를 폴드아웃 없이 그려준다.
/// </summary>
[CustomPropertyDrawer(typeof(NoFoldoutAttribute))]
public class NoFoldoutDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!property.hasVisibleChildren)
        {
            EditorGUI.PropertyField(position, property, label, false);
            return;
        }

        SerializedProperty copy = property.Copy();
        SerializedProperty end = copy.GetEndProperty();
        
        position.height = EditorGUIUtility.singleLineHeight;
        
        NoFoldoutAttribute noFoldout = attribute as NoFoldoutAttribute;
        bool includeChildren = noFoldout?.includeChildren ?? false;
        bool enterChildren = copy.hasChildren;

        while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
        {
            float height = EditorGUI.GetPropertyHeight(copy, includeChildren);
            float labelWidth = EditorGUIUtility.labelWidth / 4;
            var labelRect = new Rect(position.x, position.y, labelWidth, height);
            var propRect = new Rect(position.x + labelWidth, position.y, position.width - labelRect.width, position.height);
            
            EditorGUI.LabelField(labelRect, copy.name);
            EditorGUI.PropertyField(propRect, copy, label, includeChildren);
            position.y += height + EditorGUIUtility.standardVerticalSpacing;
            
            enterChildren = false;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.hasVisibleChildren)
        {
            return EditorGUI.GetPropertyHeight(property, label, false);
        }
        
        float totalHeight = 0f;
        SerializedProperty copy = property.Copy();
        SerializedProperty end = copy.GetEndProperty();
        
        NoFoldoutAttribute noFoldout = attribute as NoFoldoutAttribute;
        bool includeChildren = noFoldout?.includeChildren ?? false;
        bool enterChildren = copy.hasChildren;

        while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
        {
            totalHeight += EditorGUI.GetPropertyHeight(copy, includeChildren) + EditorGUIUtility.standardVerticalSpacing;
            enterChildren = false;
        }

        return totalHeight;
    }
}
