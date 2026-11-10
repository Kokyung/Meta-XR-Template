using System;
using System.Linq;
using System.Reflection;

/// <summary>
/// private과 internal 멤버에 접근을 가능하게 해주는 확장 메서드 모음
/// </summary>
public static class ReflectionUtility
{
    private static Assembly[] cachedAssemblies;

    public const BindingFlags FULL_BINDING =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// 클래스를 이름으로 찾는다.
    /// </summary>
    /// <param name="name">클래스 이름</param>
    /// <param name="assembly">어셈블리</param>
    /// <returns></returns>
    public static Type FindClass(string name)
    {
        Type result = null;

#if UNITY_EDITOR
        result = FindTypeInAssembly(name, typeof(UnityEditor.Editor).Assembly);

        if (result != null) return result;
#endif

        if (cachedAssemblies == null) cachedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < cachedAssemblies.Length; i++)
        {
            result = FindTypeInAssembly(name, cachedAssemblies[i]);

            if (result != null) return result;
        }

        return result;
    }

    /// <summary>
    /// 어셈블리 내에서 타입을 찾는다.
    /// </summary>
    /// <param name="name">타입 이름</param>
    /// <param name="assembly">대상 어셈블리</param>
    /// <returns></returns>
    private static Type FindTypeInAssembly(string name, Assembly assembly)
    {
        return assembly == null ? null : assembly.GetType(name, false, true);
    }

    /// <summary>
    /// 타입 내에서 필드를 이름으로 찾는다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="fieldName">찾을 필드 이름</param>
    /// <param name="throwNotFound">찾지 못했을때 throw 처리 여부</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="MissingFieldException"></exception>
    public static FieldInfo FindField(this Type type, string fieldName, bool throwNotFound = true)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        // 부모 클래스의 private 필드는 자식 타입에서 조회되지 않으므로 상위 타입까지 올라가며 찾는다.
        for (var current = type; current != null; current = current.BaseType)
        {
            var field = current.GetField(fieldName, FULL_BINDING);

            if (field != null) return field;
        }

        if (throwNotFound) throw new MissingFieldException(type.FullName, fieldName);

        return null;
    }

    /// <summary>
    /// 타입 내에서 프로퍼티를 찾는다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="propertyName">프로퍼티 이름</param>
    /// <param name="throwNotFound">찾지 못했을때 throw 처리 여부</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="MissingMemberException"></exception>
    public static PropertyInfo FindProperty(this Type type, string propertyName, bool throwNotFound = true)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        var prop = type.GetProperty(propertyName, FULL_BINDING);

        if (prop == null && throwNotFound) throw new MissingMemberException(type.FullName, propertyName);

        return prop;
    }

    /// <summary>
    /// 타입 내에서 메서드를 이름으로 찾는다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="args">1개 이상의 매개 변수 타입</param>
    /// <param name="throwNotFound">찾지 못했을때 throw 처리 여부</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="MissingMethodException"></exception>
    public static MethodInfo FindMethod(this Type type, string methodName, Type[] args = null,
        bool throwNotFound = true)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        MethodInfo method;

        if (args == null)
        {
            // GetMethod(name)은 오버로드가 있으면 AmbiguousMatchException을 던지므로 직접 순회한다.
            method = FindMethodByName(type, methodName, -1);
        }
        else
        {
            method = type.GetMethod(methodName, FULL_BINDING, null, args, null);

            // 윗 줄 방법으로 메서드가 제대로 바인딩 되지 않는 특수한 경우가 있다.
            // 메서드의 매개변수가 Enum인 반면 args가 int 하나인 경우, null 인자가 있는 경우에는
            // args 타입을 무시하고 매개변수 개수가 같은 메서드를 찾는다.
            if (method == null) method = FindMethodByName(type, methodName, args.Length);
        }

        if (method == null && throwNotFound) throw new MissingMethodException(type.FullName, methodName);

        return method;
    }

    /// <summary>
    /// 이름이 같은 첫 메서드를 찾는다.
    /// </summary>
    /// <param name="parameterCount">매개변수 개수. 음수이면 개수를 확인하지 않는다.</param>
    private static MethodInfo FindMethodByName(Type type, string methodName, int parameterCount)
    {
        foreach (var method in type.GetMethods(FULL_BINDING))
        {
            if (method.Name != methodName) continue;

            if (parameterCount < 0) return method;

            if (method.GetParameters().Length == parameterCount) return method;
        }

        return null;
    }

    /// <summary>
    /// 메서드 탐색용 인자 타입 배열을 만든다. null 인자는 타입을 알 수 없으므로 object로 둔다.
    /// </summary>
    private static Type[] GetArgTypes(object[] args)
    {
        if (args == null) return Type.EmptyTypes;

        return args.Select(a => a == null ? typeof(object) : a.GetType()).ToArray();
    }

    /// <summary>
    /// 타입 내에 정적 필드의 값을 이름으로 찾아 가져온다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="fieldName">정적 필드 이름</param>
    /// <returns></returns>
    public static object GetFieldValue(object obj, string fieldName)
    {
        return obj.GetType().FindField(fieldName).GetValue(obj);
    }

    /// <summary>
    /// 타입 내에 정적 필드의 값을 이름으로 찾아 가져온다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="fieldName">정적 필드 이름</param>
    /// <typeparam name="T">필드 타입</typeparam>
    /// <returns></returns>
    public static T GetFieldValue<T>(this Type type, string fieldName)
    {
        return (T) type.FindField(fieldName).GetValue(null);
    }

    /// <summary>
    /// 인스턴스 내에 필드 값을 이름으로 찾아 가져온다.
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="fieldName">필드 이름</param>
    /// <typeparam name="T">필드 타입</typeparam>
    /// <returns></returns>
    public static T GetFieldValue<T>(object obj, string fieldName)
    {
        return (T) obj.GetType().FindField(fieldName).GetValue(obj);
    }

    /// <summary>
    /// 타입 내에 정적 필드의 값을 이름으로 찾아 값을 대입한다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="fieldName">정적 필드 이름</param>
    /// <param name="value">대입할 필드 값</param>
    public static void SetFieldValue(this Type type, string fieldName, object value)
    {
        type.FindField(fieldName).SetValue(null, value);
    }

    /// <summary>
    /// 인스턴스 내에 필드 값을 이름으로 찾아 값을 대입한다.
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="fieldName">필드 이름</param>
    /// <param name="value">대입할 필드 값</param>
    public static void SetFieldValue(object obj, string fieldName, object value)
    {
        obj.GetType().FindField(fieldName).SetValue(obj, value);
    }

    /// <summary>
    /// 타입 내에 정적 프로퍼티를 이름으로 찾아 가져온다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="propertyName">프로퍼티 이름</param>
    /// <typeparam name="T">프로퍼티 타입</typeparam>
    /// <returns></returns>
    public static T GetPropertyValue<T>(this Type type, string propertyName)
    {
        return (T) type.FindProperty(propertyName).GetValue(null, null);
    }

    /// <summary>
    /// 인스턴스 내에 프로퍼티를 이름으로 찾아 가져온다.
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="propertyName">프로퍼티 이름</param>
    /// <typeparam name="T">프로퍼티 타입</typeparam>
    /// <returns></returns>
    public static T GetPropertyValue<T>(object obj, string propertyName)
    {
        return (T) obj.GetType().FindProperty(propertyName).GetValue(obj, null);
    }

    /// <summary>
    /// 타입 내에 정적 프로퍼티를 이름으로 찾아 값을 대입한다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="propertyName">프로퍼티 이름</param>
    /// <param name="value">대입할 프로퍼티 값</param>
    public static void SetPropertyValue(this Type type, string propertyName, object value)
    {
        type.FindProperty(propertyName).SetValue(null, value, null);
    }

    /// <summary>
    /// 인스턴스 내에 프로퍼티를 이름으로 찾아 값을 대입한다.
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="propertyName">프로퍼티 이름</param>
    /// <param name="value">대입할 프로퍼티 값</param>
    public static void SetPropertyValue(object obj, string propertyName, object value)
    {
        obj.GetType().FindProperty(propertyName).SetValue(obj, value, null);
    }

    /// <summary>
    /// 타입 내에 정적 메서드를 이름으로 찾아 호출하고 메서드 리턴 값을 리턴한다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="methodName">정적 메서드 이름</param>
    /// <param name="args">매개변수</param>
    /// <typeparam name="T">리턴 타입</typeparam>
    /// <returns></returns>
    public static T InvokeMethod<T>(this Type type, string methodName, params object[] args)
    {
        return (T) type.FindMethod(methodName, GetArgTypes(args)).Invoke(null, args);
    }

    /// <summary>
    /// 인스턴스 내에 메서드를 이름으로 찾아 호출하고 메서드 리턴 값을 리턴한다.
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="args">매개변수</param>
    /// <typeparam name="T">리턴 타입</typeparam>
    /// <returns></returns>
    public static T InvokeMethod<T>(object obj, string methodName, params object[] args)
    {
        return (T) obj.GetType().FindMethod(methodName, GetArgTypes(args)).Invoke(obj, args);
    }

    /// <summary>
    /// 타입 내에 정적 메서드를 이름으로 찾아 호출한다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="args">매개변수</param>
    public static void InvokeMethod(this Type type, string methodName, params object[] args)
    {
        type.FindMethod(methodName, GetArgTypes(args)).Invoke(null, args);
    }

    /// <summary>
    /// 인스턴스 내에 메서드를 이름으로 찾아 호출한다
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="args">매개변수</param>
    public static void InvokeMethod(object obj, string methodName, params object[] args)
    {
        obj.GetType().FindMethod(methodName, GetArgTypes(args)).Invoke(obj, args);
    }

    /// <summary>
    /// 주어진 타입이 다른 타입과 같으면 true를 리턴한다.
    /// </summary>
    /// <param name="toCheck">체크할 타입</param>
    /// <param name="type">비교할 타입</param>
    /// <param name="orInherited">상속 여부 체크</param>
    public static bool IsOfType(this Type toCheck, Type type, bool orInherited = true)
    {
        return type == toCheck || (orInherited && type.IsAssignableFrom(toCheck));
    }

    /// <summary>
    /// 주어진 인스턴스의 타입이 다른 타입과 같으면 true를 리턴한다.
    /// </summary>
    /// <param name="obj">체크할 인스턴스</param>
    /// <param name="type">비교할 타입</param>
    /// <param name="orInherited">상속 여부 체크</param>
    public static bool IsOfType<T>(T obj, Type type, bool orInherited = true)
    {
        return obj.GetType().IsOfType(type, orInherited);
    }

    /// <summary>
    /// 주어진 인스턴스의 타입이 다른 타입과 다르면 Exception을 던진다.
    /// </summary>
    /// <param name="obj">체크할 인스턴스</param>
    /// <param name="type">비교할 타입</param>
    /// <param name="orInherited">상속 여부 체크하여 상속되어 있으면 Exception을 던지지 않는다.</param>
    public static void EnsureOfType<T>(T obj, Type type, bool orInherited = true)
    {
        if (!IsOfType(obj, type, orInherited))
            throw new InvalidCastException(
                string.Format("Object {0} must be of type {1}{2}",
                    obj.GetType().FullName,
                    type.FullName,
                    orInherited ? " or inherited from it" : ""
                )
            );
    }

    /// <summary>
    /// 타입이 해당 이름의 정적 필드를 정의하고 있으면 true를 리턴한다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="fieldName">필드 이름</param>
    /// <returns></returns>
    public static bool HasField(this Type type, string fieldName)
    {
        return type.FindField(fieldName, false) != null;
    }

    /// <summary>
    /// 타입이 해당 이름의 정적 프로퍼티를 정의하고 있으면 true를 리턴한다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="propertyName">필드 이름</param>
    /// <returns></returns>
    public static bool HasProperty(this Type type, string propertyName)
    {
        return type.FindProperty(propertyName, false) != null;
    }

    /// <summary>
    /// 타입이 해당 이름의 정적 메서드를 정의하고 있으면 true를 리턴한다.
    /// </summary>
    /// <param name="type">대상 타입</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="args">매개변수</param>
    /// <returns></returns>
    public static bool HasMethod(this Type type, string methodName, Type[] args = null)
    {
        return type.FindMethod(methodName, args, false) != null;
    }

    /// <summary>
    /// 인스턴스가 해당 이름의 필드를 정의하고 있으면 true를 리턴한다.
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="fieldName">필드 이름</param>
    /// <returns></returns>
    public static bool HasField(object obj, string fieldName)
    {
        return obj.GetType().HasField(fieldName);
    }

    /// <summary>
    /// 인스턴스가 해당 이름의 프로퍼티를 정의하고 있으면 true를 리턴한다.
    /// </summary>
    /// <param name="obj">대상 인스턴스</param>
    /// <param name="propertyName">프로퍼티 이름</param>
    /// <returns></returns>
    public static bool HasProperty(object obj, string propertyName)
    {
        return obj.GetType().HasProperty(propertyName);
    }

    /// <summary>
    /// 인스턴스가 해당 이름의 메서드를 정의하고 있으면 true를 리턴한다.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="methodName"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool HasMethod(object obj, string methodName, Type[] args = null)
    {
        return obj.GetType().HasMethod(methodName, args);
    }
}

