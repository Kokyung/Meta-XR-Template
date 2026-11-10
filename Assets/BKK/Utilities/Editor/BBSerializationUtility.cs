using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

public static class BBSerializationUtility
{
    private const BindingFlags FIELD_BINDING =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static object GetValue(this SerializedProperty property)
    {
        object obj = property.serializedObject.targetObject;

        foreach (var step in ParsePath(property.propertyPath))
        {
            obj = GetStepValue(obj, step);
        }

        return obj;
    }

    // 중첩된 필드나 배열 요소여도 SerializedProperty가 가리키는 값을 대입한다.
    public static void SetValue(this SerializedProperty property, object val)
    {
        object obj = property.serializedObject.targetObject;

        var steps = ParsePath(property.propertyPath);
        var containers = new List<object>(steps.Count);

        foreach (var step in steps)
        {
            containers.Add(obj);
            obj = GetStepValue(obj, step);
        }

        // 값 타입 컨테이너는 복사본이므로 자식에서 부모 순서로 다시 대입한다.
        for (int i = steps.Count - 1; i >= 0; --i)
        {
            SetStepValue(containers[i], steps[i], val);
            val = containers[i];
        }
    }

    // "list.Array.data[0].value"를 필드 이름(string)과 배열 인덱스(int) 단계로 나눈다.
    private static List<object> ParsePath(string propertyPath)
    {
        var steps = new List<object>();

        foreach (var element in propertyPath.Replace(".Array.data[", "[").Split('.'))
        {
            var bracket = element.IndexOf('[');

            if (bracket < 0)
            {
                steps.Add(element);
                continue;
            }

            steps.Add(element.Substring(0, bracket));
            steps.Add(int.Parse(element.Substring(bracket + 1, element.Length - bracket - 2)));
        }

        return steps;
    }

    private static object GetStepValue(object obj, object step)
    {
        if (step is int index) return ((IList)obj)[index];

        return FindField(obj.GetType(), (string)step).GetValue(obj);
    }

    private static void SetStepValue(object obj, object step, object value)
    {
        if (step is int index) ((IList)obj)[index] = value;
        else FindField(obj.GetType(), (string)step).SetValue(obj, value);
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        // 부모 클래스의 private 필드는 자식 타입에서 조회되지 않으므로 상위 타입까지 올라가며 찾는다.
        for (var current = type; current != null; current = current.BaseType)
        {
            var field = current.GetField(fieldName, FIELD_BINDING);

            if (field != null) return field;
        }

        throw new MissingFieldException($"{type.FullName}에서 {fieldName} 필드를 찾을 수 없습니다.");
    }
}
