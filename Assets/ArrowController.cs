using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("寿命 (一定時間後に矢を消滅させる)")]
    [SerializeField] private float lifetime = 10f; // 10秒後に消滅
    //public SphereCollider proximityTrigger;
    [Header("サウンド設定")]
    [SerializeField] private AudioSource arrowAudioSource;
    [SerializeField] private AudioClip targetHitSound;     // ターゲットに当たる音
    //[SerializeField] private AudioClip otherHitSound;      // その他に当たる音

    [Header("エフェクト設定")]
    [SerializeField] private GameObject hitEffectPrefab; // ターゲットに当たったときのエフェクトプレハブ
    [SerializeField] private float hitEffectScale = 0.5f;
    //[SerializeField] private GameObject otherHitEffectPrefab; // その他に当たったときのエフェクトプレハブ（オプション）


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("ArrowScriptにはRigidbodyが必要です！");
            enabled = false; // Rigidbodyがない場合、スクリプトを無効化
        }
    }

    private void OnEnable()
    {
        // 寿命タイマーを開始
        //Invoke("DestroyArrow", lifetime);
    }

    private void Update()
    {
        // 矢が飛んでいる間、常に速度ベクトルに合わせて向きを調整する
        // これにより、矢が進行方向に常に頭を向けるように見えます。
        if (rb.velocity.magnitude > 0.1f) // 速度が十分に速い場合のみ
        {
            transform.up = rb.velocity.normalized;
        }
    }

    // IsTriggerがOnのコライダーが衝突したときに呼ばれる
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("矢のトリガーが衝突しました: " + other.gameObject.name);

        // 衝突した相手が自身でなければ処理を続行
        // (矢が他の矢や、自分自身のコライダーに反応するのを避けるため)
        if (other.gameObject == this.gameObject)
        {
            return;
        }
        PooledTarget target = other.GetComponent<PooledTarget>();
        if (target != null)
        {
            // エフェクトとサウンドを再生
            HandleEffectAndSound(other.gameObject, hitEffectPrefab, targetHitSound);
            Debug.Log("爆発によりターゲット " + other.name + " を破壊します。");
            TargetPoolManager.Instance.ReturnTarget(target); // ターゲットをプールに戻す
            DestroyArrow();
            
            enabled = false;
        }
        
        // 衝突したゲームオブジェクトが削除対象であるかを判断するロジック
        // 例: 衝突したオブジェクトが特定のタグを持っている場合
        if (other.gameObject.CompareTag("Ground")) // 例: 削除したいオブジェクトに "DeletableTarget" タグを付ける
        {
            Debug.Log("Groundに当たった！" + this.name + "削除します。");

            // --- 矢と衝突したゲームオブジェクトの両方を削除 ---
            //Destroy(other.gameObject); // 衝突したゲームオブジェクトを削除
            DestroyArrow();            // 矢自身を削除 (Invokeで設定したDestroyArrowを直接呼ぶ)

            // 衝突したらこのスクリプトのUpdate関数を停止し、再度のトリガー検出を防ぐ
            enabled = false;
        }
    }

    private void HandleEffectAndSound(GameObject targetToDestroy, GameObject effectPrefab, AudioClip soundClip)
    {

        
        // エフェクトを生成 (衝突位置に)
        GameObject effectInstance = null;
        if (effectPrefab != null)
        {
            // other.transform.position ではなく、実際に衝突した位置を使うとより自然
            // OnCollisionEnter と異なり OnTriggerEnter では contact point が直接得られないため、
            // 衝突したコライダーのClosestPointを使っておおよその衝突位置を推定する
            effectInstance = Instantiate(effectPrefab, targetToDestroy.GetComponent<Collider>().ClosestPoint(transform.position), Quaternion.identity);
            effectInstance.transform.localScale = new Vector3(hitEffectScale, hitEffectScale, hitEffectScale); // ここでスケールを適用
            Destroy(effectInstance, 1f); // エフェクトは1秒後に消す
        }

        // サウンドを再生
        if (arrowAudioSource != null && soundClip != null)
        {
            arrowAudioSource.PlayOneShot(soundClip);
        }

    }

    private void DestroyArrow()
    {
        Destroy(gameObject);
        Debug.Log("Destroy the arrow");
    }
}

