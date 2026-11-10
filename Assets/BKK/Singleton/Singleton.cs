using System;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static object _lock = new object();

    public static T Instance => GetInstance();

    // 타입이 처음 사용될 때 한 번 등록된다. Domain Reload를 끄면 등록이 유지되어 다음 플레이 시작 시 초기화된다.
    static Singleton()
    {
        SingletonStaticReset.Reset += ResetStatics;
    }

    private static void ResetStatics()
    {
        applicationIsQuitting = false;
        _instance = null;
    }

    protected static T GetInstance(bool dontDestroyOnLoad = true)
    {
        if (applicationIsQuitting)
        {
            Debug.LogWarning($"[Singleton] 앱 종료 중이므로 {typeof(T)} 싱글턴 인스턴스를 생성하지 않았습니다.");
            return null;
        }

        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = (T) FindAnyObjectByType(typeof(T));

                if (FindObjectsByType(typeof(T)).Length > 1)
                {
                    Debug.LogError($"{typeof(T)} 싱글턴 인스턴스가 둘 이상 존재합니다.");
                    return _instance;
                }

                if (_instance == null)
                {
                    var singleton = new GameObject();
                    _instance = singleton.AddComponent<T>();
                    singleton.name = typeof(T).ToString();

                    if (Application.isPlaying && dontDestroyOnLoad) DontDestroyOnLoad(singleton);
                }
                else
                {
                    Debug.Log($"{typeof(T)} 싱글턴 인스턴스가 이미 존재합니다. {_instance.gameObject.name}");

                    if (Application.isPlaying && dontDestroyOnLoad && !_instance.gameObject.IsDontDestroyOnLoad())
                    {
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }
            }

            return _instance;
        }

    }

    private static bool applicationIsQuitting = false;

    protected static void InitInstance(bool dontDestroyOnLoad = true)
    {
        GetInstance(dontDestroyOnLoad);
    }

    protected virtual void OnDestroy()
    {
        applicationIsQuitting = true;
    }
}

/// <summary>
/// Domain Reload를 끈 에디터에서는 이전 플레이의 정적 필드가 남아, 종료 시 켜진 applicationIsQuitting 때문에
/// 다음 플레이에서 싱글턴이 생성되지 않는다. 씬 로드 전에 사용된 모든 Singleton 타입의 정적 상태를 초기화한다.
/// </summary>
// RuntimeInitializeOnLoadMethod는 제네릭 클래스에서 호출되지 않으므로 비제네릭 클래스에서 각 타입에 알린다.
internal static class SingletonStaticReset
{
    public static event Action Reset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnSubsystemRegistration()
    {
        Reset?.Invoke();
    }
}