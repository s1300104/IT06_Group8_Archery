using UnityEngine;
using System.Collections;

public class PooledTarget : MonoBehaviour, IPoolable
{
    public float lifeTime = 5f;
    private Coroutine lifeCoroutine;

    [Header("Effects (Assign in Inspector)")]
    [Tooltip("ターゲットのスポーン時に出現するエフェクト")]
    public GameObject spawnEffectPrefab;
    [Tooltip("ターゲットの自然消滅時に出現するエフェクト")]
    public GameObject naturalDespawnEffectPrefab; // 名前を変更して区別
    [Tooltip("ターゲットがプレイヤーアクションで破壊された時に出現するエフェクト")]
    public GameObject playerActionDespawnEffectPrefab; // 新規追加
    [Tooltip("スポーン時の効果音")]
    public AudioClip spawnSound;
    [Tooltip("自然消滅時の効果音")]
    public AudioClip naturalDespawnSound; // 名前を変更
    [Tooltip("プレイヤーアクションで破壊された時の効果音")]
    public AudioClip playerActionDespawnSound; // 新規追加

    protected TargetMovement targetMovement; // TargetMovementスクリプトへの参照

    protected virtual void Awake()
    {
        // 自身のGameObjectにアタッチされているTargetMovementを取得
        targetMovement = GetComponent<TargetMovement>();
        if (targetMovement == null)
        {
            Debug.LogError("TargetMovement component not found on PooledTarget!", this);
        }
    }

    public virtual void OnSpawn()
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

        // エフェクト生成
        if (spawnEffectPrefab != null && this.GetType() == typeof(PooledTarget))
        {
            GameObject effect = Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        // 効果音生成
        if (spawnSound != null && this.GetType() == typeof(PooledTarget))
        {
            AudioSource.PlayClipAtPoint(spawnSound, transform.position, 0.5f); // 簡易的な再生
        }
    }

    public virtual void OnDespawn(DespawnReason reason)
    {
        // コルーチン停止
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        lifeCoroutine = null;

        // TargetMovementのコルーチンも停止
        if (targetMovement != null)
        {
            targetMovement.StopAllMovementCoroutines();
        }

        // デスポーン理由に応じてエフェクトとサウンドを再生
        PlayDespawnEffects(reason);

        // TargetSpawner のカウントデクリメント
        var spawner = FindObjectOfType<TargetSpawner>(); // パフォーマンス向上のため、可能ならキャッシュまたは別の方法で参照を取得
        spawner?.OnTargetDespawned(this);
                                     // エフェクト停止など後片付け

    }

    // ★追加: デスポーン理由に応じたエフェクト再生メソッド
    protected virtual void PlayDespawnEffects(DespawnReason reason)
    {
        GameObject effectToDespawn = null;
        AudioClip soundToPlay = null;
        float soundVolume = 0.5f;

        // this.GetType() == typeof(PooledTarget) のチェックは、
        // BonusTargetでこのメソッドをオーバーライドして専用エフェクトを出す場合に、
        // ここで通常ターゲットのエフェクトのみを処理するため。
        // もしBonusTargetがこのメソッドをオーバーライドしないなら、このチェックは不要で、
        // reasonだけで分岐すれば良い。
        // ここでは、BonusTargetがこのメソッドをオーバーライドすることを想定し、
        // PooledTargetの場合は通常のエフェクトを再生するようにする。
        // （ただし、BonusTargetがbase.PlayDespawnEffects()を呼ばない限り）

        // if (this is PooledTarget) // 通常ターゲットまたは継承先でオーバーライドされていない場合
        // {
            switch (reason)
            {
                case DespawnReason.Natural:
                    effectToDespawn = naturalDespawnEffectPrefab;
                    soundToPlay = naturalDespawnSound;
                    // soundVolume = 0.4f; // 自然消滅は少し音量を下げるなど
                    Debug.Log($"Target {gameObject.name} despawning naturally.");
                    break;
                case DespawnReason.PlayerAction:
                    effectToDespawn = playerActionDespawnEffectPrefab;
                    soundToPlay = playerActionDespawnSound;
                    // soundVolume = 0.6f; // プレイヤーアクションは少し音量を上げるなど
                    Debug.Log($"Target {gameObject.name} destroyed by player action.");
                    break;
                case DespawnReason.OutOfBounds:
                case DespawnReason.ForceRemoved:
                    // これらの場合はエフェクトなし、または共通の消滅エフェクト
                    // effectToDespawn = genericVanishEffect;
                    // soundToPlay = genericVanishSound;
                    Debug.Log($"Target {gameObject.name} despawning due to: {reason}");
                    break;
            }
        // }


        if (effectToDespawn != null)
        {
            GameObject effect = Instantiate(effectToDespawn, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, transform.position, soundVolume);
        }
    }

    public void UpdatePosition(Vector3 pos)
    {
        transform.position = pos;
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        if (TargetPoolManager.Instance != null)
        {
            // 寿命によるデスポーンなので、Naturalを指定
            TargetPoolManager.Instance.ReturnTarget(this, DespawnReason.Natural); // ★修正: 理由を指定
        }
        else if (gameObject.activeSelf)
        {
            gameObject.SetActive(false); // フォールバック
        }
    }
}