using UnityEngine;

public static class GameObjectExtension
{
    public static T FindComponentRecursivelyByName<T>(this Transform root, string name)
    {
        Transform target = root.FindRecursively(name);

        return target ? target.GetComponent<T>() : default;
    }

    public static float GetAngle(Vector3 from, Vector3 to)
    {
        // Vector3 v = to - from;
        // return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.z;
    }

    public static bool IsHandBone(this HumanBodyBones bone)
    {
        return bone is HumanBodyBones.LeftHand or HumanBodyBones.RightHand;
    }

    public static bool IsFingerBone(this HumanBodyBones bone)
    {
        return (int) bone >= 24 && (int) bone <= 53;
    }

    public static bool IsFootBone(this HumanBodyBones bone)
    {
        return bone == HumanBodyBones.LeftFoot | bone == HumanBodyBones.RightFoot;
    }

    public static bool IsToeBone(this HumanBodyBones bone)
    {
        return bone == HumanBodyBones.LeftToes | bone == HumanBodyBones.RightToes;
    }

    public static void SetAllLayer(this GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            child.gameObject.layer = layer;

            Transform _HasChildren = child.GetComponentInChildren<Transform>();
            if (_HasChildren != null) SetAllLayer(child.gameObject, layer);
        }
    }

    public static bool IncludedInLayerMask(this GameObject target, LayerMask layerMask)
    {
        return ((1 << target.gameObject.layer) & layerMask) != 0;
    }

    public static bool ExcludedInLayerMask(this GameObject target, LayerMask layerMask)
    {
        return ((1 << target.gameObject.layer) & layerMask) == 0;
    }

    public static bool IsSceneObject(this GameObject target)
    {
        return target.scene.name != null;
    }

    public static bool IsDontDestroyOnLoad(this GameObject target)
    {
        return target.scene.buildIndex == -1;
    }

    public static Transform FindRecursively(this Transform target, string name)
    {
        foreach (Transform child in target)
        {
            if (child.name.ToLower() == name.ToLower())
            {
                return child;
            }
            else
            {
                var result = child.FindRecursively(name);

                if (result != null) return result;
            }
        }

        return null;
    }

    public static T FindRecursively<T>(this Transform target, string name)
    {
        foreach (Transform child in target)
        {
            if (child.name.ToLower() == name.ToLower())
            {
                if (child.TryGetComponent(out T type))
                {
                    return type;
                }
                else
                {
                    return default(T);
                }
            }
            else
            {
                var result = child.FindRecursively(name);

                if (result != null)
                {
                    if (result.TryGetComponent(out T type))
                    {
                        return type;
                    }
                    else
                    {
                        return default(T);
                    }
                }
            }
        }

        return default(T);
    }

    public static Transform FindIncludeInactive(this Transform target, string name)
    {
        foreach (Transform child in target)
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    public static T GetComponentByGameObjectName<T>(this GameObject target, string name)
    {
        var result = target.transform.FindRecursively(name);

        return result.GetComponent<T>();
    }

    public static T TryGetComponentInParent<T>(this GameObject go, out T comp)
    {
        comp = default;
        return go.GetComponentInParent<T>();
    }

    public static T TryGetComponentInChildren<T>(this GameObject go, out T comp)
    {
        comp = default;
        return go.GetComponentInChildren<T>();
    }

    public static bool IsMissingReference(this UnityEngine.Object unknown)
    {
        try
        {
            unknown.GetEntityId();
            return false;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
            return true;
        }
    }

    public static void AdoptChildren(this Transform to, Transform from, bool resetLocalScale = true)
    {
        foreach (Transform child in from)
        {
            child.SetParent(to);
            if (resetLocalScale) child.localScale = Vector3.one;
        }
    }

    public static void SetChildren(this Transform target, Transform[] children)
    {
        foreach (var child in children)
        {
            target.SetChild(child);
        }
    }

    public static void SetChild(this Transform target, Transform child)
    {
        child.SetParent(target);
    }

    public static Transform[] GetChildren(this Transform target)
    {
        Transform[] children = new Transform[target.childCount];

        for (var i = 0; i < target.childCount; i++)
        {
            children[i] = target.GetChild(i);
        }

        return children;
    }

    public static bool ContainsScreenPoint(this RectTransform rectTransform, Vector3 pos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pos);
    }

    public static bool ContainsScreenPoint(this RectTransform rectTransform, Vector3 pos, Camera eventCamera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pos, eventCamera);
    }

    public static bool ContainsScreenPoint(this RectTransform rectTransform, Vector3 pos, Camera eventCamera,
        Vector4 offset)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pos, eventCamera, offset);
    }

    public static Vector2 WorldToScreenPoint(this Vector3 worldPoint, Camera eventCamera)
    {
        return RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);
    }

    public static void Destroy(Object obj)
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            Object.DestroyImmediate(obj);
        }
        else
        {
            Object.Destroy(obj);
        }
    }
    
    public static bool IsInCollider(this Vector3 point, Collider collider)
    {
        Vector3 closestPoint = collider.ClosestPoint(point);
        
        return Vector3.Distance(closestPoint, point) < 0.001f;
    }

    public static bool IsInCollider(this Vector3 point, Collider2D collider)
    {
        Vector3 closestPoint = collider.ClosestPoint(point);
        
        return Vector3.Distance(closestPoint, point) < 0.001f;
    }

    public static bool IsInCollider(this Vector3 point, Collider2D collider, out Vector2 closestPoint)
    {
        closestPoint = collider.ClosestPoint(point);
        
        return Vector3.Distance(closestPoint, point) < 0.001f;
    }
}

