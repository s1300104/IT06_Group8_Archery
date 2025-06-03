// GrenadeSocket.cs
using UnityEngine;

public class GrenadeSocket : MonoBehaviour
{
    [Tooltip("グレネードを配置する実際の位置と向きを示すTransform(アタッチポイント)")]
    public Transform spawnPoint; // インスペクターで設定

    public bool IsEmpty { get; private set; } = true;
    private Grenade currentGrenadeInSocket;

    void Awake()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("GrenadeSocket: spawnPointが設定されていません!", this);
            enabled = false; // spawnPointがないと機能しない
        }
    }

    /// <summary>
    /// 指定されたグレネードをこのソケットに配置します。
    /// </summary>
    public void PlaceGrenade(Grenade grenadeInstance)
    {
        if (grenadeInstance == null) return;

        currentGrenadeInSocket = grenadeInstance;
        IsEmpty = false;
        // グレネードの位置と向きをspawnPointに合わせる (Instantiate時に行うことが多いが念のため)
        grenadeInstance.transform.position = spawnPoint.position;
        grenadeInstance.transform.rotation = spawnPoint.rotation;
    }

    /// <summary>
    /// このソケットからグレネードが持ち去られたことを通知されます。
    /// </summary>
    public void OnGrenadeTaken()
    {
        currentGrenadeInSocket = null;
        IsEmpty = true;
        Debug.Log($"Socket {gameObject.name} is now empty.");
    }

    /// <summary>
    /// 現在このソケットに入っているグレネードインスタンスを返します（デバッグや特殊な処理用）。
    /// </summary>
    public Grenade GetGrenadeInSocket()
    {
        return currentGrenadeInSocket;
    }
}