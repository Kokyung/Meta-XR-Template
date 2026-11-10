using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
public class SerializableDictionaryEditor : PropertyDrawer
{
    private ReorderableList list;

    private SerializedProperty keysProperty;
    private SerializedProperty valuesProperty;

    private bool initialized = false;

    private bool unfolded = false;

    ~SerializableDictionaryEditor()
    {
        initialized = false;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // base.OnGUI(position, property, label);

        if (!initialized) Initialize(property);

        EditorGUI.BeginProperty(position, label, property);

        // // 자식 필드 표시 제한
        // var indent = EditorGUI.indentLevel;
        // EditorGUI.indentLevel = 0;

        // Debug.Log(EditorGUI.indentLevel);

        string prefsName = $"BKK.DataStructure.SerializableDictionary_{label}";

        unfolded = EditorPrefs.GetBool(prefsName, true);

        unfolded = EditorGUI.Foldout(position, unfolded, label);

        if (unfolded)
        {
            list.DoLayoutList();
            // var rect = new Rect(position.x, position.y, position.width, 300);
            // list.DoList(rect, rect);
        }

        EditorPrefs.SetBool(prefsName, unfolded);

        // // 자식 필드 표시 제한 해제
        // EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    private void Initialize(SerializedProperty property)
    {
        initialized = true;

        keysProperty = property.FindPropertyRelative("keys");
        valuesProperty = property.FindPropertyRelative("values");

        list = new ReorderableList(property.serializedObject, keysProperty, true, false, true, true)
        {
            drawElementCallback = (rect, index, active, focused) =>
            {
                var labelWidth = EditorGUIUtility.labelWidth / 3f - EditorGUIUtility.standardVerticalSpacing;
                var keyWidth = ((rect.width - (labelWidth * 2)) / 2) - EditorGUIUtility.standardVerticalSpacing * 2;
                var valueWidth = rect.width - labelWidth - keyWidth - labelWidth - EditorGUIUtility.singleLineHeight;
                var keyPosX = rect.x + labelWidth - EditorGUIUtility.standardVerticalSpacing;
                var valueLabelPosX = keyPosX + keyWidth + EditorGUIUtility.standardVerticalSpacing * 2;
                var valuePosX = valueLabelPosX + labelWidth + EditorGUIUtility.singleLineHeight -
                                EditorGUIUtility.standardVerticalSpacing;

                var elementKey = keysProperty.GetArrayElementAtIndex(index);
                var elementValue = valuesProperty.GetArrayElementAtIndex(index);

                var elementKeyHeight = EditorGUI.GetPropertyHeight(elementKey, true);
                var elementValueHeight = EditorGUI.GetPropertyHeight(elementValue, true);

                var elementHeight = elementKeyHeight > elementValueHeight ? elementKeyHeight : elementValueHeight;

                var keyLabelRect = new Rect(rect.x, rect.y, labelWidth, elementHeight);
                var keyRect = new Rect(keyPosX, rect.y, keyWidth, elementHeight);
                var valueLabelRect = new Rect(valueLabelPosX, rect.y, labelWidth, elementHeight);
                var valueRect = new Rect(valuePosX, rect.y, valueWidth, elementHeight);

                EditorGUILayout.BeginVertical();
                
                EditorGUI.LabelField(keyLabelRect, $"Key {index}");

                EditorGUI.PropertyField(keyRect, elementKey, GUIContent.none, true);

                EditorGUI.LabelField(valueLabelRect, $"Value {index}");

                EditorGUI.PropertyField(valueRect, elementValue, GUIContent.none, true);

                EditorGUILayout.EndVertical();
            },
            elementHeightCallback = (index) =>
            {
                var elementKey = keysProperty.GetArrayElementAtIndex(index);
                var elementValue = valuesProperty.GetArrayElementAtIndex(index);

                var elementKeyHeight = EditorGUI.GetPropertyHeight(elementKey, true);
                var elementValueHeight = EditorGUI.GetPropertyHeight(elementValue, true);

                var elementHeight = elementKeyHeight > elementValueHeight ? elementKeyHeight : elementValueHeight;

                return elementHeight;
            },
            onAddCallback = (list) =>
            {
                keysProperty.arraySize++;
                valuesProperty.arraySize++;
            },
            onRemoveCallback = (list) =>
            {
                keysProperty.arraySize--;
                valuesProperty.arraySize--;
            },
            onReorderCallbackWithDetails = (list, index, newIndex) =>
            {
                property.serializedObject.Update();

                keysProperty.MoveArrayElement(index, newIndex);
                valuesProperty.MoveArrayElement(index, newIndex);

                property.serializedObject.ApplyModifiedProperties();
            },
        };
    }
}
