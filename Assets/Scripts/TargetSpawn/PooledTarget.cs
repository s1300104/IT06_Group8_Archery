using UnityEngine;

public class PooledTarget : MonoBehaviour, IPoolable
{
    public float lifeTime = 5f;
    private float timer;

    public void OnSpawn()
    {
        timer = 0f;
        // 色や大きさのランダム化など初期化処理を実装
    }

    public void OnDespawn()
    {
        // エフェクト停止など、返却時の後片付け
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            // 自分自身をプールに返却
            TargetPoolManager.Instance.ReturnTarget(this);
            // TargetSpawner のカウントをデクリメント
            TargetSpawner spawner = FindObjectOfType<TargetSpawner>();
            if (spawner != null)
            {
                spawner.OnTargetDespawned();
            }
        }
    }
}