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
            Debug.Log("爆発によりターゲット " + other.name + " を破壊します。");
            TargetPoolManager.Instance.ReturnTarget(target); // ターゲットをプールに戻す
        }
        DestroyArrow();
        // 衝突したゲームオブジェクトが削除対象であるかを判断するロジック
        // 例: 衝突したオブジェクトが特定のタグを持っている場合
        /*
        if (other.gameObject.CompareTag("Box")) // 例: 削除したいオブジェクトに "DeletableTarget" タグを付ける
        {
            Debug.Log("DeletableTargetに当たった！削除します。");

            // --- 矢と衝突したゲームオブジェクトの両方を削除 ---
            Destroy(other.gameObject); // 衝突したゲームオブジェクトを削除
            DestroyArrow();            // 矢自身を削除 (Invokeで設定したDestroyArrowを直接呼ぶ)

            // 衝突したらこのスクリプトのUpdate関数を停止し、再度のトリガー検出を防ぐ
            enabled = false;
        }
        /*
        else if (other.gameObject.CompareTag("Target")) // 前回の例の「Target」タグなど
        {
            // ここにターゲットに刺さるロジックを続ける
            Debug.Log("矢がターゲットに当たった！");

            // 物理演算を無効化し、ターゲットの子オブジェクトにする
            rb.isKinematic = true;
            transform.SetParent(other.transform); // OnTriggerEnterではCollisionではなくColliderが渡されるため、other.transformを使用

            // 衝突したらスクリプトを無効化 (Updateでの向き調整を停止)
            enabled = false; // これにより、Update関数が呼ばれなくなる

            // 必要であれば、一定時間後に矢を消す
            Invoke("DestroyArrow", 5f); // 5秒後にターゲットに刺さった矢を消す
        }
        else
        {
            // その他のオブジェクトに当たった場合（地面、壁など）
            Debug.Log("矢がターゲット以外のものに当たった: " + other.gameObject.name);

            // 物理演算を停止し、その場に留まらせる（刺さるように見せる）
            rb.isKinematic = true;
            // 矢の寿命を短くするなど
            Invoke("DestroyArrow", 2f); // 2秒後に消す
            enabled = false; // Update停止
        }
        */
    }

    private void DestroyArrow()
    {
        Destroy(gameObject);
    }
}

