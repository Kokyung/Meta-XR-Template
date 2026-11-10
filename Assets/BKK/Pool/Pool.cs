using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BKK.Pool
{
    /// <summary>
    /// 프리팹 단위로 인스턴스화된 GameObject를 재사용합니다.
    /// </summary>
    public static class Pool
    {
        private const int DefaultCapacity = 8;
        private const int DefaultMaxSize = 64;

        private static readonly Dictionary<int, PoolBucket> buckets = new();
        private static readonly Dictionary<int, PoolBucket> instanceBuckets = new();
        private static PoolRunner runner;

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            if (!prefab)
            {
                Debug.LogError("풀(Pool)에서 null 프리팹을 생성할 수 없습니다.");
                return null;
            }

            var bucket = GetOrCreateBucket(prefab);
            var instance = bucket.Get(position, rotation, parent);
            var id = (int) EntityId.ToULong(instance.GetEntityId());
            instanceBuckets[id] = bucket;
            return instance;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, int defaultCapacity,
            int maxSize, Transform parent = null)
        {
            if (!prefab)
            {
                Debug.LogError("풀(Pool)에서 null 프리팹을 생성할 수 없습니다.");
                return null;
            }

            var bucket = GetOrCreateBucket(prefab, defaultCapacity, maxSize);
            var instance = bucket.Get(position, rotation, parent);
            var id = (int) EntityId.ToULong(instance.GetEntityId());
            instanceBuckets[id] = bucket;
            return instance;
        }

        public static GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            if (!prefab)
            {
                Debug.LogError("풀(Pool)에서 null 프리팹을 생성할 수 없습니다.");
                return null;
            }

            return Spawn(prefab.gameObject, position, rotation, parent).GetComponent<T>();
        }

        public static T Spawn<T>(T prefab, Transform parent = null) where T : Component
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public static bool Release(GameObject instance)
        {
            if (!instance) return false;

            var id =  (int) EntityId.ToULong(instance.GetEntityId());
            
            if (!instanceBuckets.TryGetValue(id, out var bucket))
            {
                Debug.LogWarning($"'{instance.name}'은(는) Pool에 의해 생성되지 않았으므로 Pool로 반환할 수 없습니다.", instance);
                return false;
            }

            bucket.Release(instance);
            return true;
        }

        public static bool Release(Component instance)
        {
            return instance && Release(instance.gameObject);
        }

        public static void ReleaseAfter(GameObject instance, float delay)
        {
            if (!instance || delay < 0f) return;
            GetRunner().StartCoroutine(ReleaseAfterRoutine(instance, delay));
        }

        public static void ReleaseAfter(Component instance, float delay)
        {
            if (instance) ReleaseAfter(instance.gameObject, delay);
        }

        public static void Prewarm(GameObject prefab, int count)
        {
            if (!prefab || count <= 0) return;

            GetOrCreateBucket(prefab).Prewarm(count);
        }

        public static void Create(GameObject prefab, int defaultCapacity, int maxSize)
        {
            if (!prefab)
            {
                Debug.LogError("null 프리팹에 대한 풀을 생성할 수 없습니다.");
                return;
            }

            GetOrCreateBucket(prefab, defaultCapacity, maxSize);
        }

        public static void Clear(GameObject prefab)
        {
            if (!prefab) return;
            
            var id = (int) EntityId.ToULong(prefab.GetEntityId());
            
            if (!buckets.TryGetValue(id, out var bucket)) return;

            bucket.Clear();
            buckets.Remove(id);
        }

        public static void ClearAll()
        {
            foreach (var bucket in buckets.Values)
            {
                bucket.Clear();
            }

            buckets.Clear();
            instanceBuckets.Clear();
        }

        private static IEnumerator ReleaseAfterRoutine(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            Release(instance);
        }

        private static PoolBucket GetOrCreateBucket(GameObject prefab)
        {
            return GetOrCreateBucket(prefab, DefaultCapacity, DefaultMaxSize);
        }

        private static PoolBucket GetOrCreateBucket(GameObject prefab, int defaultCapacity, int maxSize)
        {
            var prefabId = (int) EntityId.ToULong(prefab.GetEntityId());
            if (buckets.TryGetValue(prefabId, out var bucket)) return bucket;

            defaultCapacity = Mathf.Max(0, defaultCapacity);
            maxSize = Mathf.Max(1, maxSize);
            defaultCapacity = Mathf.Min(defaultCapacity, maxSize);

            bucket = new PoolBucket(prefab, defaultCapacity, maxSize, GetRunner().transform);
            buckets.Add(prefabId, bucket);
            return bucket;
        }

        private static PoolRunner GetRunner()
        {
            if (runner) return runner;

            var poolRoot = new GameObject("Pool");
            Object.DontDestroyOnLoad(poolRoot);
            runner = poolRoot.AddComponent<PoolRunner>();
            return runner;
        }

        private sealed class PoolBucket
        {
            private readonly GameObject prefab;
            private readonly Queue<GameObject> available;
            private readonly HashSet<int> activeInstanceIds = new();
            private readonly int maxSize;
            private readonly Transform inactiveParent;

            public PoolBucket(GameObject prefab, int defaultCapacity, int maxSize, Transform poolRoot)
            {
                this.prefab = prefab;
                available = new Queue<GameObject>(defaultCapacity);
                this.maxSize = maxSize;

                var poolParent = new GameObject($"{prefab.name} Pool").transform;
                poolParent.SetParent(poolRoot);
                inactiveParent = poolParent;
            }

            public GameObject Get(Vector3 position, Quaternion rotation, Transform parent)
            {
                GameObject instance = null;
                while (available.Count > 0 && !instance)
                {
                    instance = available.Dequeue();
                }

                if (!instance)
                {
                    instance = Object.Instantiate(prefab);
                    instance.name = prefab.name;
                }

                var id = (int)EntityId.ToULong(instance.GetEntityId());
                
                activeInstanceIds.Add(id);
                var instanceTransform = instance.transform;
                instanceTransform.SetParent(parent, false);
                instanceTransform.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
                return instance;
            }

            public void Release(GameObject instance)
            {
                var instanceId = (int)EntityId.ToULong(instance.GetEntityId());
                if (!activeInstanceIds.Remove(instanceId)) return;

                if (available.Count >= maxSize)
                {
                    instanceBuckets.Remove(instanceId);
                    Object.Destroy(instance);
                    return;
                }

                instance.SetActive(false);
                instance.transform.SetParent(inactiveParent, false);
                available.Enqueue(instance);
            }

            public void Prewarm(int count)
            {
                var createCount = Mathf.Min(count, maxSize - available.Count - activeInstanceIds.Count);
                for (var i = 0; i < createCount; i++)
                {
                    var instance = Object.Instantiate(prefab, inactiveParent);
                    var id = (int)EntityId.ToULong(instance.GetEntityId());
                    
                    instance.name = prefab.name;
                    instance.SetActive(false);
                    available.Enqueue(instance);
                    instanceBuckets[id] = this;
                }
            }

            public void Clear()
            {
                while (available.Count > 0)
                {
                    var instance = available.Dequeue();
                    if (!instance) continue;
                    
                    var id = (int)EntityId.ToULong(instance.GetEntityId());
                    
                    instanceBuckets.Remove(id);
                    Object.Destroy(instance);
                }

                foreach (var instanceId in activeInstanceIds)
                {
                    instanceBuckets.Remove(instanceId);
                }

                activeInstanceIds.Clear();
                Object.Destroy(inactiveParent.gameObject);
            }
        }

        private sealed class PoolRunner : MonoBehaviour
        {
            private void OnDestroy()
            {
                if (runner != this) return;

                runner = null;
                buckets.Clear();
                instanceBuckets.Clear();
            }
        }
    }
}