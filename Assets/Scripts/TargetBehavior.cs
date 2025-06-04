// TargetBehavior.cs （例として）
using UnityEngine;

public class TargetBehavior : MonoBehaviour
{
    public int scoreValue = 10; // このターゲットを破壊したときのスコア

    // ターゲットがヒットしたときに呼ばれるメソッド（例: 矢が当たった時など）
    public void OnHit()
    {
        Debug.Log("Target Hit!");

        // GameManagerのAddScoreメソッドを呼び出してスコアを加算
        if (GameManager.Instance != null) // GameManagerのインスタンスが存在するか確認
        {
            GameManager.Instance.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("GameManager instance not found. Cannot add score.");
        }

        // ターゲットを破壊する
        Destroy(gameObject);
    }
}