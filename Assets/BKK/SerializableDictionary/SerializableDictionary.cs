using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에디터 표시가 가능한 딕셔너리.
/// </summary>
/// <typeparam name="TKey">Key</typeparam>
/// <typeparam name="TValue">Value</typeparam>
[System.Serializable]
[Obsolete("Custom SerializableDictionary는 더 이상 Unity 6에서 사용되지 않습니다. 대신 Dictionary에 SerializeField를 선언하여 사용하세요.")]
public class SerializableDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    [SerializeField, NoFoldout] private List<TKey> keys = new List<TKey>();
    [SerializeField, NoFoldout] private List<TValue> values = new List<TValue>();

    public List<TKey> Keys => keys;
    public List<TValue> Values => values;

    public void Add(TKey key, TValue value)
    {
        if (keys.Contains(key))
            throw new System.Exception("Key가 이미 존재합니다.");
        keys.Add(key);
        values.Add(value);
    }

    public bool TryAdd(TKey key, TValue value)
    {
        if (keys.Contains(key))
        {
            return false;
        }

        try
        {
            keys.Add(key);
            values.Add(value);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Remove(TKey key)
    {
        if (!keys.Contains(key))
            throw new System.Exception("키가 존재하지 않습니다.");

        values.Remove(values[keys.IndexOf(key)]);
        keys.Remove(key);
    }

    public TValue this[TKey key]
    {
        get
        {
            if (!keys.Contains(key))
                throw new System.Exception("Key doesn't exists");
            return values[keys.IndexOf(key)];
        }
        set
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
                values.Add(default);
            }
            // throw new System.Exception("Key doesn't exists");

            int index = keys.IndexOf(key);
            values[index] = value;
        }

    }

    public KeyValuePair<TKey, TValue> this[int index]
    {
        get
        {
            if (keys.Count < index)
                throw new System.IndexOutOfRangeException();
            return new KeyValuePair<TKey, TValue>(keys[index], values[index]);
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        int keyIndex = keys.IndexOf(key);

        if (keyIndex < 0)
        {
            value = default(TValue);
            return false;
        }

        TValue valueTemp = values[keyIndex];

        if (values.Contains(valueTemp))
        {
            value = valueTemp;
            return true;
        }
        else
        {
            value = default(TValue);
            return false;
        }
    }

    public bool ContainsKey(TKey key)
    {
        return keys.Contains(key);
    }

    public bool ContainsValue(TValue value)
    {
        return values.Contains(value);
    }

    public void Clear()
    {
        keys.Clear();
        values.Clear();
    }

    public TKey FindKey(Predicate<TKey> match)
    {
        return keys.Find(match);
    }

    public TValue FindValue(Predicate<TValue> match)
    {
        return values.Find(match);
    }

    public int Count => keys.Count;

    public List<TValue> ToList()
    {
        return values;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        for (int i = 0; i < keys.Count; i++)
        {
            yield return new KeyValuePair<TKey, TValue>(keys[i], values[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

