using UnityEngine;

public class SingletonScriptableObject<T> : ScriptableObject where T : SingletonScriptableObject<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                T[] assets = Resources.LoadAll<T>("SingletonSO");

                if (assets == null || assets.Length == 0)
                {
                    throw new System.Exception("리소스 폴더에 싱글턴 스크립터블 오브젝트가 존재하지 않습니다.");
                }
                
                if (assets.Length > 1)
                {
                    Debug.LogWarning($"여러 개의 {nameof(T)} 싱글턴 스크립터블 오브젝트가 감지되었습니다. 첫번째로 로드된 오브젝트를 사용합니다.");
                }

                instance = assets[0];
            }

            return instance;
        }
    }
}
