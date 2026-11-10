using System;

public static class NullUtility
{
    /// <summary>
    /// Null이면 Func의 리턴 값을 대입합니다. 파괴된 UnityEngine.Object도 Null로 취급합니다.
    /// </summary>
    /// <param name="targetProperty"></param>
    /// <param name="func"></param>
    /// <typeparam name="TResult"></typeparam>
    public static void InsertIfNull<TResult>(ref TResult targetProperty, Func<TResult> func)
    {
        if (IsNull(targetProperty))
        {
            targetProperty = func.Invoke();
        }
    }

    private static bool IsNull<T>(T value)
    {
        // 파괴된 UnityEngine.Object는 C# 참조가 남아 있으므로 Unity의 == 연산자로 비교한다.
        if (value is UnityEngine.Object unityObject) return unityObject == null;

        return value == null;
    }
}
