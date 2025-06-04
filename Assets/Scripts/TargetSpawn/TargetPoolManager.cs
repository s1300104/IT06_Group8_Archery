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

    [Header("Player Reference")]
    public Transform playerTransform; // プレイヤーのTransformを設定

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

        // プレイヤーの方向を向く
        LookAtPlayer(t.transform);

        return t;
    }

    // ボーナスターゲットの生成
    public BonusTarget SpawnBonusTarget(Vector3 position)
    {
        if (bonusTargetPool == null)
        {
            Debug.LogError("Bonus Target Poolが初期化されていないか、Prefabが設定されていません。");
            return null;
        }
        var t = bonusTargetPool.Get();
        t.transform.position = position;

        // プレイヤーの方向を向く
        LookAtPlayer(t.transform);

        return t;
    }

    // ターゲットがプレイヤーの方向を向く処理
    private void LookAtPlayer(Transform targetTransform)
    {
        if (playerTransform != null)
        {
            Vector3 lookAtPosition = playerTransform.position;
            lookAtPosition.y = targetTransform.position.y; // ターゲットのY座標を固定してLookAt
            targetTransform.LookAt(lookAtPosition);
        }
    }

    // ターゲットの返却
    public void ReturnTarget(PooledTarget t, DespawnReason reason)
    {
        if (t == null) return;

        if (t is BonusTarget bonusTarget && bonusTargetPool != null)
        {
            bonusTargetPool.ReturnToPool(bonusTarget, reason);
        }
        else if (regularTargetPool != null)
        {
            regularTargetPool.ReturnToPool(t, reason);
        }
        else
        {
            Debug.LogWarning($"Target {t.gameObject.name} could not be returned to any pool. Destroying directly.");
            Destroy(t.gameObject);
        }
    }

    public void ReturnTarget(PooledTarget t)
    {
        ReturnTarget(t, DespawnReason.Natural); // デフォルトはNaturalとする
    }
}