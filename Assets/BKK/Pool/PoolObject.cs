using UnityEngine;
using UnityEngine.Pool;

public abstract class PoolObject<T> : MonoBehaviour where T : MonoBehaviour
{
    protected IObjectPool<T> managedPool;
    
    public void RegisterManagedPool(IObjectPool<T> pool)
    {
        managedPool = pool;
    }

    public void DestroyPoolObject()
    {
        managedPool.Release(this as T);
    }
}
