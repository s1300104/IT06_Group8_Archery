using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(FixedJoint))] // FixedJointが必須
public class GrenadePin : MonoBehaviour
{
    [Tooltip("ピンが引き抜かれた後に消滅するまでの時間（秒）")]
    public float disappearDelay = 3.0f;

    private Grenade parentGrenade; // 親となるグレネードのスクリプトへの参照
    private Rigidbody rb;
    private FixedJoint joint;
    private bool isPulled = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        joint = GetComponent<FixedJoint>();

        // 親オブジェクトからGrenadeスクリプトを取得することを試みる
        // ピンがグレネードの子として正しく設定されている必要がある
        if (transform.parent != null)
        {
            parentGrenade = transform.parent.GetComponent<Grenade>();
        }

        if (parentGrenade == null)
        {
            Debug.LogError("GrenadePin: 親オブジェクトにGrenadeスクリプトが見つかりません。ピンはグレネードの子である必要があります。", this);
            enabled = false; // スクリプトを無効化
            return;
        }

        // FixedJointのConnectedBodyが親グレネードのRigidbodyに設定されていることを確認（エディタでの設定を推奨）
        if (joint.connectedBody == null && parentGrenade.GetComponent<Rigidbody>() != null)
        {
            joint.connectedBody = parentGrenade.GetComponent<Rigidbody>();
            // Debug.Log("GrenadePin: FixedJointのConnectedBodyを親グレネードのRigidbodyに自動設定しました。");
        }
        else if (joint.connectedBody == null)
        {
            Debug.LogError("GrenadePin: FixedJointのConnectedBodyが設定されておらず、親グレネードにRigidbodyもありません。", this);
        }
    }

    // FixedJointが破壊されたときに呼び出される
    void OnJointBreak(float breakForce)
    {
        if (isPulled) return; // 既に処理済みなら何もしない
        isPulled = true;

        Debug.Log("ピンが引き抜かれました！");

        if (parentGrenade != null)
        {
            parentGrenade.OnPinPulled(); // 親グレネードにピンが抜かれたことを通知
        }

        // ピンが本体から分離したので、もし親子関係が残っていれば解除する（通常は不要）
        // transform.SetParent(null); // 物理挙動のために独立させる

        // Rigidbodyが物理法則に従うように設定（AwakeでUseGravity=On, IsKinematic=Offを推奨）
        // rb.isKinematic = false;
        // rb.useGravity = true;

        // 一定時間後に自身を消滅させるコルーチンを開始
        StartCoroutine(DisappearRoutine());
    }

    private IEnumerator DisappearRoutine()
    {
        yield return new WaitForSeconds(disappearDelay);
        Destroy(gameObject);
    }

    // (オプション) プレイヤーがピンを掴んだ際に、JointのBreakForceを一時的に下げて抜きやすくするなどの処理も考えられる
    // public void OnPinGrabbed() { /* joint.breakForce = lowValue; */ }
    // public void OnPinReleased() { /* joint.breakForce = originalValue; */ }
}