using UnityEngine;
using System.Collections;

public class PooledTarget : MonoBehaviour, IPoolable
{
    public float lifeTime = 5f;
    private Coroutine lifeCoroutine;

    private TargetMovement targetMovement; // TargetMovementスクリプトへの参照

    void Awake()
    {
        // 自身のGameObjectにアタッチされているTargetMovementを取得
        targetMovement = GetComponent<TargetMovement>();
        if (targetMovement == null)
        {
            Debug.LogError("TargetMovement component not found on PooledTarget!", this);
        }
    }

    public void OnSpawn()
    {
        // ライフタイムコルーチン開始（前回あれば停止）
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        lifeCoroutine = StartCoroutine(LifeTimer());

        // TargetMovementがアタッチされていれば、移動パターンを初期化
        if (targetMovement != null)
        {
            // MovementPattern enumの全ての値を取得し、ランダムに一つ選択
            var movementPatterns = System.Enum.GetValues(typeof(TargetMovement.MovementPattern));
            TargetMovement.MovementPattern randomPattern = (TargetMovement.MovementPattern)movementPatterns.GetValue(Random.Range(0, movementPatterns.Length));

            // TargetMovementを初期化
            targetMovement.InitializeMovement(randomPattern, transform.position);
        }

        // その他リセット処理（色やスケールなど）
        // 例: GetComponent<Renderer>().material.color = Color.white;
    }

    public void OnDespawn()
    {
        // コルーチン停止
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        lifeCoroutine = null;

        // TargetMovementのコルーチンも停止
        if (targetMovement != null)
        {
            targetMovement.StopAllMovementCoroutines();
        }

        // TargetSpawner のカウントデクリメント
        var spawner = FindObjectOfType<TargetSpawner>(); // パフォーマンス向上のため、可能ならキャッシュまたは別の方法で参照を取得
        spawner?.OnTargetDespawned();
                                     // エフェクト停止など後片付け
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        // 寿命到達でプール返却
        TargetPoolManager.Instance.ReturnTarget(this);
    }
}