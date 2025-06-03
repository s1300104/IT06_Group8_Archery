using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("寿命 (一定時間後に矢を消滅させる)")]
    [SerializeField] private float lifetime = 10f; // 10秒後に消滅

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
            transform.forward = rb.velocity.normalized;
        }
    }

/*
    private void OnCollisionEnter(Collision collision)
    {
        // 衝突した相手が「Target」タグを持っているか、特定のレイヤーにいるかなどをチェック
        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log("矢がターゲットに当たった！");

            // 衝突した位置にパーティクルエフェクトを生成 (オプション)
            // GameObject hitEffect = Instantiate(hitParticlePrefab, collision.contacts[0].point, Quaternion.identity);
            // Destroy(hitEffect, 2f); // 2秒後にエフェクトを消す

            // サウンドエフェクトを再生 (オプション)
            // AudioManager.PlaySound("TargetHit");

            // 矢をターゲットに刺さったように見せる
            // 物理演算を無効化し、ターゲットの子オブジェクトにする
            rb.isKinematic = true;
            transform.SetParent(collision.transform);

            // 衝突したらスクリプトを無効化 (Updateでの向き調整を停止)
            enabled = false; // これにより、Update関数が呼ばれなくなる

            // 必要であれば、一定時間後に矢を消す
            Invoke("DestroyArrow", 5f); // 5秒後にターゲットに刺さった矢を消す
        }
        else
        {
            // ターゲット以外のものに当たった場合
            // 例えば、地面に落ちた場合なども物理演算を停止させる
            rb.isKinematic = true;
            // 矢の寿命を短くするなど
            Invoke("DestroyArrow", 2f); // 2秒後に消す
            enabled = false; // Update停止
        }
    }
    */

    private void DestroyArrow()
    {
        Destroy(gameObject);
    }
}

