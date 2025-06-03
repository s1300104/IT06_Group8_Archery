// GrenadeSocketSpawner.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GrenadeSocketSpawner : MonoBehaviour
{
    [Tooltip("生成するグレネードのプレハブ")]
    public GameObject grenadePrefab;

    [Tooltip("管理するグレネードソケットのリスト（インスペクターで設定）")]
    public List<GrenadeSocket> sockets; // インスペクターで設定 [詳：]

    [Tooltip("グレネードを生成する間隔（秒）")]
    public float generationInterval = 5.0f;

    void Awake()
    {
        if (grenadePrefab == null)
        {
            Debug.LogError("GrenadeSocketSpawner: grenadePrefabが設定されていません！", this);
            enabled = false;
            return;
        }
        if (sockets == null || sockets.Count == 0)
        {
            Debug.LogError("GrenadeSocketSpawner: socketsリストが設定されていないか空です！", this);
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        BonusTarget.OnBonusTargetDestroyed_Global += HandleBonusTargetDestroyed; // イベント購読 [詳：]
    }

    void OnDisable()
    {
        BonusTarget.OnBonusTargetDestroyed_Global -= HandleBonusTargetDestroyed; // イベント購読解除 [詳：]
    }

    void Start()
    {
        StartCoroutine(GenerateGrenadesRoutine());
    }

    private IEnumerator GenerateGrenadesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(generationInterval);
            AttemptToSpawnOneGrenadeInNextAvailableSocket();
        }
    }

    private void AttemptToSpawnOneGrenadeInNextAvailableSocket()
    {
        foreach (GrenadeSocket socket in sockets) // インスペクターで設定された順序（昇順）で処理
        {
            if (socket.IsEmpty)
            {
                if (GrenadeManager.Instance != null && GrenadeManager.Instance.CanSpawnGrenade())
                {
                    GameObject newGrenadeObject = Instantiate(grenadePrefab, socket.spawnPoint.position, socket.spawnPoint.rotation); // アタッチポイントを利用 [詳：]
                    Grenade newGrenade = newGrenadeObject.GetComponent<Grenade>();

                    if (newGrenade != null)
                    {
                        // GrenadeManagerへの登録はGrenade.csのStartで行われる
                        socket.PlaceGrenade(newGrenade);
                        newGrenade.InitializeInSocket(socket); // グレネードにソケット情報を渡す
                        Debug.Log($"Grenade spawned in socket: {socket.gameObject.name}");
                    }
                    else
                    {
                        Debug.LogError("GrenadeSocketSpawner: 生成したオブジェクトにGrenadeコンポーネントがありません。", newGrenadeObject);
                        Destroy(newGrenadeObject); // 不正なオブジェクトは破棄
                    }
                    return; // 一回の生成サイクルで1つ生成 [詳：]
                }
                else
                {
                    Debug.Log("GrenadeSocketSpawner: グレネードの最大数に達しているか、GrenadeManagerが見つかりません。");
                    return; // 生成できない場合はこのサイクルを終了
                }
            }
        }
        Debug.Log("GrenadeSocketSpawner: 空きソケットがありません。");
    }

    private void HandleBonusTargetDestroyed()
    {
        Debug.Log("ボーナスターゲット破壊検知！グレネードをリフィルします。");
        RefillAllSockets();
    }

    private void RefillAllSockets()
    {
        foreach (GrenadeSocket socket in sockets)
        {
            if (socket.IsEmpty)
            {
                if (GrenadeManager.Instance != null && GrenadeManager.Instance.CanSpawnGrenade())
                {
                    GameObject newGrenadeObject = Instantiate(grenadePrefab, socket.spawnPoint.position, socket.spawnPoint.rotation);
                    Grenade newGrenade = newGrenadeObject.GetComponent<Grenade>();
                    if (newGrenade != null)
                    {
                        socket.PlaceGrenade(newGrenade);
                        newGrenade.InitializeInSocket(socket);
                        Debug.Log($"Grenade refilled in socket: {socket.gameObject.name} by bonus.");
                    }
                    else
                    {
                       Debug.LogError("GrenadeSocketSpawner (Refill): 生成したオブジェクトにGrenadeコンポーネントがありません。", newGrenadeObject);
                       Destroy(newGrenadeObject);
                    }
                }
                else
                {
                    Debug.Log("GrenadeSocketSpawner (Refill): グレネードの最大数に達しているか、GrenadeManagerが見つかりません。リフィルを中断します。");
                    break; // 最大数に達したらリフィルを中断
                }
            }
        }
    }
}