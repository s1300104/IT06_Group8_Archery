using UnityEngine;

public class TargetPoolManager : MonoBehaviour
{
    public static TargetPoolManager Instance { get; private set; }

    [Header("Regular Target")]
    public PooledTarget targetPrefab;
    public int initialPoolSize = 20;
    private ObjectPool<PooledTarget> regularTargetPool;

    [Header("Bonus Target")]
    public BonusTarget bonusTargetPrefab; // ボーナスターゲットのプレハブ
    public int initialBonusPoolSize = 5;  // ボーナスターゲットの初期プールサイズ
    private ObjectPool<BonusTarget> bonusTargetPool;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (targetPrefab != null)
        {
            regularTargetPool = new ObjectPool<PooledTarget>(targetPrefab, initialPoolSize, transform);
        }
        else
        {
            Debug.LogError("TargetPoolManager: Regular Target Prefabが設定されていません!", this);
        }

        if (bonusTargetPrefab != null)
        {
            bonusTargetPool = new ObjectPool<BonusTarget>(bonusTargetPrefab, initialBonusPoolSize, transform);
        }
        else
        {
            Debug.LogWarning("TargetPoolManager: Bonus Target Prefabが設定されていません。ボーナスターゲットはプールから生成できません。", this);
        }
    }

    // 通常ターゲットの生成
    public PooledTarget SpawnRegularTarget(Vector3 position)
    {
        if (regularTargetPool == null)
        {
            Debug.LogError("Regular Target Poolが初期化されていません。");
            return null;
        }
        var t = regularTargetPool.Get();
        t.transform.position = position;
        // OnSpawnはGet()内部で呼ばれる
        return t;
    }

    public BonusTarget SpawnBonusTarget(Vector3 position)
    {
        if (bonusTargetPool == null)
        {
            Debug.LogError("Bonus Target Poolが初期化されていないか、Prefabが設定されていません。");
            return null;
        }
        var t = bonusTargetPool.Get();
        t.transform.position = position;
        // OnSpawnはGet()内部で呼ばれる (BonusTargetのオーバーライドされたOnSpawnが呼ばれる)
        return t;
    }

    // ターゲットの返却 
    // (PooledTargetとBonusTargetの両方を受け取れるようにジェネリック化またはオーバーロードも検討可能だが、
    // PooledTargetが基底クラスなので、このメソッドでBonusTargetも扱える)
    public void ReturnTarget(PooledTarget t) // BonusTargetもPooledTargetなのでこのメソッドでOK
    {
        if (t == null) return;

        if (t is BonusTarget bonusTarget && bonusTargetPool != null)
        {
            bonusTargetPool.ReturnToPool(bonusTarget);
        }
        else if (regularTargetPool != null) // BonusTargetでなかった場合、またはbonusTargetPoolが何らかの理由でnullの場合
        {
            regularTargetPool.ReturnToPool(t);
        }
        else
        {
            // どちらのプールにも返せない場合 (エラーまたは直接破棄)
            Debug.LogWarning($"Target {t.gameObject.name} could not be returned to any pool. Destroying directly.");
            Destroy(t.gameObject);
        }
    }
}