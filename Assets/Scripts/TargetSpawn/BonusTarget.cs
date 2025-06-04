// BonusTarget.cs
using UnityEngine;
using System; // System.Actionのため

public class BonusTarget : PooledTarget // PooledTargetを継承
{
    public static event Action OnBonusTargetDestroyed_Global;

    [Header("Bonus Target Settings")]
    [Tooltip("このボーナスターゲットに使用する特定の移動パターン。")]
    public TargetMovement.MovementPattern movementPatternForBonus = TargetMovement.MovementPattern.Straight; // 例: デフォルトは直線移動

    [Tooltip("trueの場合、上記の移動パターンで PooledTarget のデフォルトのランダムな移動パターンを上書きします。")]
    public bool overrideDefaultMovement = true;

    [Header("Bonus Target Effects (Overrides PooledTarget if set)")]
    public GameObject bonusSpawnEffectPrefab;
    public GameObject bonusNaturalDespawnEffectPrefab;
    public GameObject bonusPlayerActionDespawnEffectPrefab;
    public AudioClip bonusSpawnSound;
    public AudioClip bonusNaturalDespawnSound;
    public AudioClip bonusPlayerActionDespawnSound;

    // PooledTargetのOnSpawnをオーバーライドして、独自の移動パターンを設定
    public override void OnSpawn()
    {
        base.OnSpawn(); // まず親クラス(PooledTarget)のOnSpawnを呼び出す (LifeTimerの開始など) [cite: 69, 307]

        if (overrideDefaultMovement && targetMovement != null) // targetMovementはPooledTargetから継承 [cite: 70, 308]
        {
            // PooledTargetのOnSpawnで設定された可能性のある移動パターンを上書き
            targetMovement.InitializeMovement(movementPatternForBonus, transform.position);
            // Debug.Log($"BonusTarget {gameObject.name} spawned with specific pattern: {movementPatternForBonus}");
        }

        // ボーナス専用スポーンエフェクト (もしあれば)
        if (bonusSpawnEffectPrefab != null)
        {
            Instantiate(bonusSpawnEffectPrefab, transform.position, Quaternion.identity);
        }
        if (bonusSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(bonusSpawnSound, transform.position, 0.55f);
        }
    }

    // PooledTargetのOnDespawnをオーバーライドして、イベントを発行
    public override void OnDespawn(DespawnReason reason)
    {
        // ボーナスターゲットが「破壊された」とみなし、イベントを発行
        // このタイミングは、ライフタイムでの消滅、またはプレイヤーによる破壊など、
        // プールに戻る全てのケースでイベントを発行するかどうかを検討する必要があります。
        // ここでは、OnDespawn時にイベントを発行するとします。
        Debug.Log($"BonusTarget {gameObject.name} is being despawned/destroyed. Invoking global event.");
        if (reason == DespawnReason.PlayerAction) OnBonusTargetDestroyed_Global?.Invoke();

        base.OnDespawn(reason); // 親クラス(PooledTarget)のOnDespawnを呼び出す (移動停止、TargetSpawnerへの通知など) [cite: 77, 315]
    }

    // ボーナスターゲット専用のデスポーンエフェクト処理 (PooledTargetのものをオーバーライド)
    protected override void PlayDespawnEffects(DespawnReason reason)
    {
        GameObject effectToDespawn = null;
        AudioClip soundToPlay = null;
        float soundVolume = 0.5f;

        // ボーナス専用エフェクトが設定されていればそれを使う
        switch (reason)
        {
            case DespawnReason.Natural:
                effectToDespawn = bonusNaturalDespawnEffectPrefab != null ? bonusNaturalDespawnEffectPrefab : naturalDespawnEffectPrefab;
                soundToPlay = bonusNaturalDespawnSound != null ? bonusNaturalDespawnSound : naturalDespawnSound;
                // soundVolume = 0.45f;
                break;
            case DespawnReason.PlayerAction:
                effectToDespawn = bonusPlayerActionDespawnEffectPrefab != null ? bonusPlayerActionDespawnEffectPrefab : playerActionDespawnEffectPrefab;
                soundToPlay = bonusPlayerActionDespawnSound != null ? bonusPlayerActionDespawnSound : playerActionDespawnSound;
                // soundVolume = 0.65f;
                break;
            default: // OutOfBounds, ForceRemoved
                // ボーナスの場合も共通の消滅エフェクトを使うか、専用のものがあればそれを使う
                // effectToDespawn = ...
                // soundToPlay = ...
                break;
        }
        Debug.Log($"Bonus Target {gameObject.name} playing despawn effects for reason: {reason}");

        if (effectToDespawn != null)
        {
            Instantiate(effectToDespawn, transform.position, Quaternion.identity);
        }
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, transform.position, soundVolume);
        }
    }

    // 例: ボーナスターゲットがプレイヤーの攻撃などで直接破壊された場合の処理
    // 通常のライフタイムや他の要因でOnDespawnが呼ばれる前にこれを呼び出す必要がある場合。
    public void DestroyByPlayerAction()
    {
        if (!gameObject.activeSelf) return; // すでに非アクティブなら何もしない

        // ここで破壊時の特別なエフェクトやスコア処理などを行うことができる
        Debug.Log($"BonusTarget {gameObject.name} explicitly destroyed by player action.");

        // OnDespawnを直接呼び出すか、TargetPoolManager経由でプールに戻すことで
        // OnDespawn内のイベント発行とクリーンアップ処理を誘発する。
        // どちらが良いかは設計次第だが、プール管理を一元化するならTargetPoolManager経由が望ましい。
        // TargetPoolManager.Instance.ReturnTarget(this); // もしBonusTargetも同じプール/メソッドで管理する場合
        // または、専用のReturnBonusTargetメソッドがあるならそれを使用。
        // ここでは、OnDespawnが最終的に呼ばれることを期待し、直接的な重複コードは避ける。
        // 例えば、ヒットポイントが0になったらgameObject.SetActive(false)を呼び、
        // それがLifeTimerと連動するか、直接TargetPoolManager.ReturnTargetを呼ぶなど。
        // 現状のPooledTargetはLifeTimerで戻るので、直接破壊の場合は即時プールに戻すのが良いでしょう。

        // 一旦、明示的な破壊もOnDespawn経由でイベントが発火すると仮定し、
        // このメソッドは追加的な破壊エフェクトやスコア処理を行う場所とする。
        // そして最終的にプールに戻す処理 (例: gameObject.SetActive(false) や LifeTimerの即時終了) を行う。
        // もしライフタイムと無関係に即時破壊＆イベント発行が必要なら、
        // OnBonusTargetDestroyed_Global?.Invoke(); をここで行い、
        // base.OnDespawn(); の呼び出しは行わず、TargetPoolManager.Instance.ReturnTarget(this); を直接呼ぶ。
        // ただし、イベントはOnDespawnで一元化するのがシンプルかもしれない。
        // 今回はOnDespawnでイベント発行しているので、ここでは追加処理のみ。
        // そして、プールに戻す(実質的にOnDespawnをトリガーする)処理を行う。
        if (TargetPoolManager.Instance != null)
        {
            TargetPoolManager.Instance.ReturnTarget(this); // これがOnDespawnを呼び出す
        }
        else
        {
            gameObject.SetActive(false); // フォールバック
        }
    }
}