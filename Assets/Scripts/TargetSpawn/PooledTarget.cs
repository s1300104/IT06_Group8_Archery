using UnityEngine;
using System.Collections;

public class PooledTarget : MonoBehaviour, IPoolable
{
    public float lifeTime = 5f;
    private Coroutine lifeCoroutine;

    public void OnSpawn()
    {
        // ライフタイムコルーチン開始（前回あれば停止）
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        lifeCoroutine = StartCoroutine(LifeTimer());

        // その他リセット処理（色やスケールなど）
    }

    public void OnDespawn()
    {
        // コルーチン停止
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        lifeCoroutine = null;

        // TargetSpawner のカウントデクリメント
        var spawner = FindObjectOfType<TargetSpawner>();
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