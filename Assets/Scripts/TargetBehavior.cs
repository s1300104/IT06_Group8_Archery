using UnityEngine;

public class TargetBehavior : MonoBehaviour
{
    public int scoreValue = 10; // このターゲットを破壊したときのスコア

    // OnHit() メソッドは他のスクリプトから直接呼ばれる場合にも対応できるように残す
    public void OnHit()
    {
        Debug.Log("Target OnHit() called for: " + gameObject.name + " at " + Time.time);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("GameManager instance not found. Cannot add score from " + gameObject.name);
        }

        // ターゲットを破壊する
        Destroy(gameObject);
    }

    // ★★★ ここから追加/修正 ★★★
    // 矢との衝突を検知する
    void OnTriggerEnter(Collider other)
    {
        // 衝突したオブジェクトが「Arrow」タグを持っているかチェック
        if (other.CompareTag("Arrow"))
        {
            Debug.Log($"Target ({gameObject.name}) detected trigger with Arrow ({other.gameObject.name}).");

            // OnHit() メソッドを呼び出す
            OnHit();

            // 矢も破壊する場合 (ゲームプレイによって調整)
            // このDestroyはWBStringController.csでは制御されないため、ここで破壊する
            // 注意: 矢が貫通しないようにするには、OnHit()の前にArrowCollisionHandler.csでRigidbody.isKinematicをtrueにするなどの処理が必要
            Destroy(other.gameObject);
        }
    }

    // もし矢のColliderのIs Triggerをfalseにしている場合、OnCollisionEnterを使用
    // ただし、WBStringControllerでisTrigger=trueにしているので、通常OnTriggerEnterでOK
    /*
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Arrow"))
        {
            Debug.Log($"Target ({gameObject.name}) detected collision with Arrow ({collision.gameObject.name}).");
            OnHit();
            Destroy(collision.gameObject);
        }
    }
    */
    // ★★★ ここまで追加/修正 ★★★
}