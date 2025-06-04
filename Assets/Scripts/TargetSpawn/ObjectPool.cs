using UnityEngine;
using System.Collections.Generic;

public class ObjectPool<T> where T : Component, IPoolable
{
    private T prefab;
    private Transform parent;
    private Queue<T> pool = new Queue<T>();

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateNew();
            ReturnToPool(obj);
        }
    }

    private T CreateNew()
    {
        var instance = Object.Instantiate(prefab, parent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    public T Get()
    {
        var obj = pool.Count > 0 ? pool.Dequeue() : null;
        if (obj == null) return null;
        obj.gameObject.SetActive(true);
        obj.OnSpawn();
        return obj;
    }

    // ReturnToPoolメソッドがDespawnReasonを受け取るように修正
    public void ReturnToPool(T obj, DespawnReason reason) // ★修正: 引数にDespawnReason reason を追加
    {
        obj.OnDespawn(reason); // reasonを渡す
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    public void ReturnToPool(T obj) // 引数なし版（デフォルトはNaturalなど）
    {
        ReturnToPool(obj, DespawnReason.Natural); // デフォルトの理由を設定
    }
}