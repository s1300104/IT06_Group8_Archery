using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class Grenade : XRGrabInteractable
{
    [Header("State")]
    private bool hasPinPulled = false;
    private bool isExploded = false;

    [Header("Explosion Settings")]
    [Tooltip("爆発がターゲットを検知する半径")]
    public float explosionRadius = 5.0f;
    [Tooltip("ターゲット近接検知用のトリガーコライダー（SphereColliderを想定）")]
    public SphereCollider proximityTrigger; // インスペクターで設定
    // public float explosionForce = 700f; // 他のRigidbodyに力を加える場合（今回はターゲット破壊のみ）

    [Header("Effects (Assign in Inspector)")]
    [Tooltip("爆発時に生成するパーティクルエフェクトのプレハブ")]
    public GameObject explosionEffectPrefab;
    [Tooltip("ピン抜き時の効果音")]
    public AudioClip pinPullSound;
    [Tooltip("爆発時の効果音")]
    public AudioClip explosionSound;
    // private AudioSource audioSource; // AudioSourceコンポーネントで音を再生する場合

    void Awake()
    {
        base.Awake();
        
        // audioSource = GetComponent<AudioSource>(); // AudioSourceがない場合は追加する
        // if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (proximityTrigger == null)
        {
            // 必須ではないが、警告を出す（近接爆発が機能しないため）
            Debug.LogWarning("Grenade: Proximity Triggerが設定されていません。ターゲット近接による爆発は機能しません。", this);
        }
        else
        {
            proximityTrigger.isTrigger = true; // 念のためトリガーに設定
            proximityTrigger.radius = explosionRadius; // 爆発半径とトリガー半径を一致させる (または別途設定)
        }
    }

    void Start()
    {
        // GrenadeManagerに自身を登録
        if (GrenadeManager.Instance != null)
        {
            // 本来はInstantiateする側がCanSpawnGrenade()を確認すべきだが、
            // ここでは、万が一上限を超えて生成された場合に自身を破棄するなどのフォールバックも考えられる
            // （今回は、生成側が責任を持つ前提で、単純に登録のみ）
            GrenadeManager.Instance.RegisterGrenade();
        }
        else
        {
            Debug.LogError("Grenade: GrenadeManagerのインスタンスが見つかりません！", this);
        }
    }

    /// <summary>
    /// GrenadePinから呼び出され、ピンが抜かれたことを処理します。
    /// </summary>
    public void OnPinPulled()
    {
        if (isExploded || hasPinPulled) return; // 既に爆発済みかピンが抜かれていれば何もしない

        hasPinPulled = true;
        Debug.Log("グレネードのピンが抜かれ、爆発準備完了。");

        // ピン抜き効果音再生
        if (pinPullSound != null /*&& audioSource != null*/)
        {
            // audioSource.PlayOneShot(pinPullSound);
            AudioSource.PlayClipAtPoint(pinPullSound, transform.position); // 簡易的な再生
        }

        // (オプション) ピンが抜けたら近接検知トリガーを有効にするなど
        if (proximityTrigger != null)
        {
            proximityTrigger.enabled = true;
        }
    }

    // ターゲットが近接検知トリガーに入ったときに呼び出される
    void OnTriggerEnter(Collider other)
    {
        if (!hasPinPulled || isExploded) return; // ピンが抜かれていないか、既に爆発済みなら何もしない

        // ターゲットかどうかを確認 (PooledTargetコンポーネントを持つかなど)
        PooledTarget target = other.GetComponent<PooledTarget>();
        if (target != null)
        {
            Debug.Log("ターゲット (" + other.name + ") がグレネードの近接範囲に入りました。爆発します。");
            Explode();
        }
    }

    private void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        Debug.Log("グレネード爆発！");

        // 爆発エフェクト生成
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 爆発音再生
        if (explosionSound != null /*&& audioSource != null*/)
        {
            // audioSource.PlayOneShot(explosionSound);
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f); // 簡易的な再生 (音量1.0f)
        }

        // 範囲内のコライダーを取得
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            // ターゲット破壊処理
            PooledTarget target = hit.GetComponent<PooledTarget>();
            if (target != null)
            {
                Debug.Log("爆発によりターゲット " + hit.name + " を破壊します。");
                TargetPoolManager.Instance.ReturnTarget(target); // ターゲットをプールに戻す
            }

            // (オプション) 他のRigidbodyに爆風の力を加える
            // Rigidbody rb = hit.GetComponent<Rigidbody>();
            // if (rb != null)
            // {
            //     rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            // }
        }

        // グレネード自身を破棄
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // GrenadeManagerから自身を登録解除
        if (GrenadeManager.Instance != null)
        {
            GrenadeManager.Instance.UnregisterGrenade();
        }
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        // 既に他のインタラクターによって選択されているか（保持されているか）どうかを確認
        // interactorsSelecting リストには、現在このオブジェクトを選択しているインタラクターが含まれる
        bool isAlreadyGrabbedByOther = interactorsSelecting.Count > 0 && !interactorsSelecting.Contains(interactor);
    
        if (isAlreadyGrabbedByOther)
        {
            // 既に他の手で掴まれている場合、このインタラクター（別の手）による選択は許可しない
            return false;
        }
    
        // 上記以外の場合（誰も掴んでいない、または掴もうとしているのが現在掴んでいる手自身であるなど）、
        // 基底クラスの選択ロジック（距離やレイヤーマスクなど）に従う
        return base.IsSelectableBy(interactor);
    }

    // (デバッグ用) 爆発範囲をシーンビューに表示
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        if (proximityTrigger != null && proximityTrigger.enabled)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityTrigger.radius);
        }
    }
}