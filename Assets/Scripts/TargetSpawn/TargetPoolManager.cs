using UnityEngine;

public class TargetPoolManager : MonoBehaviour
{
    public static TargetPoolManager Instance { get; private set; }
    public PooledTarget targetPrefab;
    public int initialPoolSize = 20;

    private ObjectPool<PooledTarget> pool;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        pool = new ObjectPool<PooledTarget>(targetPrefab, initialPoolSize, transform);
    }

    public PooledTarget SpawnTarget(Vector3 position)
    {
        var t = pool.Get();
        t.transform.position = position;
        return t;
    }

    public void ReturnTarget(PooledTarget t)
    {
        pool.ReturnToPool(t);
    }
}