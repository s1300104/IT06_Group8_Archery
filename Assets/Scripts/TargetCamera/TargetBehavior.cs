using UnityEngine;

public class TargetBehavior : MonoBehaviour
{
    private Transform _playerCameraTransform;

    public void SetPlayerTarget(Transform playerTransform)
    {
        Debug.Log("Player position: " + _playerCameraTransform.position + " Target position: " + transform.position);
        _playerCameraTransform = playerTransform;
        LookAtPlayerOnceCustom();
    }

    void LookAtPlayerOnceCustom()
    {
        if (_playerCameraTransform != null)
        {
            // ターゲットからプレイヤーへの方向ベクトルを計算
            Vector3 directionToPlayer = _playerCameraTransform.position - transform.position;

            // Y軸の傾きを無視して水平方向の向きだけを計算
            directionToPlayer.y = 0; // これでY軸の回転のみに制限される

            // 回転を作成 (方向ベクトルと、オブジェクトのY軸が常に上を向くように指定)
            // Quaternion.LookRotation(forward, upwards)
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up);

            // ターゲットの回転を適用
            transform.rotation = targetRotation;

            // ★ もしモデルの正面がローカルZ軸と一致しない場合は、ここで補正回転を加える ★
            // 例: モデルの正面がローカルX軸を向いている場合
            // transform.Rotate(0, -90, 0, Space.Self);
        }
    }

    // 必要に応じて、ターゲットのライフサイクルやヒット処理などをここに追加
    public void OnHit()
    {
        Debug.Log("Target Hit!");
        Destroy(gameObject);
    }
}