using System.Collections;
using UnityEngine;

/// <summary>
/// Monobehaviour를 상속받지 않은 클래스에서 코루틴을 호출할 수 있도록 해주는 클래스입니다.
///
/// 작성자: 변고경
/// </summary>
public sealed class CoroutineHelper : MonoBehaviour
{
    private static MonoBehaviour monoInstance;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var go = new GameObject($"[{nameof(CoroutineHelper)}]", typeof(CoroutineHelper))
        {
            hideFlags = HideFlags.HideInHierarchy
        };
        
        monoInstance = go.GetComponent<CoroutineHelper>();
        DontDestroyOnLoad(monoInstance.gameObject);
    }
    
    public new static Coroutine StartCoroutine(IEnumerator routine)
    {
        StartCoroutineLog(routine);
        return monoInstance.StartCoroutine(routine);
    }

    public new static void StopCoroutine(Coroutine coroutine)
    {
        monoInstance.StopCoroutine(coroutine);
    }

    public new static void StopAllCoroutines()
    {
        monoInstance.StopAllCoroutines();
    }

    private static void StartCoroutineLog(IEnumerator coroutine)
    {
        var nameData = ExtractName(coroutine);
        
        var str = $"[CoroutineHelper] IEnumerator {nameData[1]} in {nameData[0]} Class is called by CoroutineHelper";
        
        BKKDebug.Log(str);
    }

    private static string[] ExtractName(IEnumerator coroutine)
    {
        // ToString Rule: className+<IEnumeratorName>d__7

        var coroutineString = coroutine.ToString();
        var className = coroutineString.Split('+')[0];
        var methodName = coroutineString.Split('<')[1].Split('>')[0];

        return new[] {className, methodName};
    }
}
