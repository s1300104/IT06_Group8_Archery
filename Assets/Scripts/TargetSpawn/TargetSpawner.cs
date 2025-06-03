// TargetSpawner.cs
using UnityEngine;
using System.Collections;

public class TargetSpawner : MonoBehaviour
{
    [Header("General Settings")]
    public float spawnInterval = 2.0f;

    [Header("Regular Target Settings")]
    public int maxRegularTargets = 8; // 通常ターゲットの最大数
    private int currentRegularTargetCount = 0;

    [Header("Bonus Target Settings")]
    public int maxBonusTargets = 2;   // ボーナスターゲットの最大数
    [Range(0f, 1f)]
    public float bonusTargetSpawnChance = 0.1f; // ボーナスターゲットの生成確率
    private int currentBonusTargetCount = 0;

    [Header("Common Spawn Area")]
    public Vector3 spawnAreaCenter;
    public Vector3 spawnAreaSize;

    void Start()
    {
        StartCoroutine(SpawnTargetCoroutine());
    }

    IEnumerator SpawnTargetCoroutine()
    {
        while (true)
        {
            // このコルーチンは常にSpawnの試行を行う
            // Spawnメソッド内で各種上限を確認する
            Spawn();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void Spawn()
    {
        if (TargetPoolManager.Instance == null)
        {
            Debug.LogError("TargetSpawner: TargetPoolManager Instance not found!");
            return;
        }

        // どの種類のターゲットを生成試行するか (ボーナス優先、または確率で)
        bool trySpawnBonus = Random.value < bonusTargetSpawnChance;

        if (trySpawnBonus && TargetPoolManager.Instance.bonusTargetPrefab != null)
        {
            if (currentBonusTargetCount < maxBonusTargets)
            {
                SpawnSpecificTarget(true); // ボーナスターゲットを生成
                return; // このサイクルでは1体のみ生成試行 (成功したら終了)
            }
            // ボーナスの上限に達している場合は、フォールバックして通常ターゲットの生成を試みる (オプション)
            // else if (currentRegularTargetCount < maxRegularTargets) {
            //     SpawnSpecificTarget(false);
            // }
        }
        
        // 通常ターゲットの生成試行 (ボーナスを試行しなかった、またはフォールバックしなかった場合)
        if (currentRegularTargetCount < maxRegularTargets && TargetPoolManager.Instance.targetPrefab != null)
        {
            SpawnSpecificTarget(false); // 通常ターゲットを生成
        }
    }

    void SpawnSpecificTarget(bool isBonus)
    {
        var x = Random.Range(spawnAreaCenter.x - spawnAreaSize.x / 2, spawnAreaCenter.x + spawnAreaSize.x / 2);
        var y = Random.Range(spawnAreaCenter.y - spawnAreaSize.y / 2, spawnAreaCenter.y + spawnAreaSize.y / 2);
        var z = Random.Range(spawnAreaCenter.z - spawnAreaSize.z / 2, spawnAreaCenter.z + spawnAreaSize.z / 2);
        var pos = new Vector3(x, y, z);

        if (isBonus)
        {
            BonusTarget bonusTarget = TargetPoolManager.Instance.SpawnBonusTarget(pos);
            if (bonusTarget != null)
            {
                currentBonusTargetCount++;
                // Debug.Log("Bonus Target Spawned. Count: " + currentBonusTargetCount);
            }
        }
        else
        {
            PooledTarget regularTarget = TargetPoolManager.Instance.SpawnRegularTarget(pos);
            if (regularTarget != null)
            {
                currentRegularTargetCount++;
                // Debug.Log("Regular Target Spawned. Count: " + currentRegularTargetCount);
            }
        }
    }

    // PooledTarget (およびそれを継承するBonusTarget) のOnDespawnから呼び出される
    // 引数としてデスポーンしたターゲットのインスタンスを受け取る
    public void OnTargetDespawned(PooledTarget despawnedTarget)
    {
        if (despawnedTarget is BonusTarget)
        {
            if (currentBonusTargetCount > 0)
            {
                currentBonusTargetCount--;
                Debug.Log("Bonus Target Despawned. Count: " + currentBonusTargetCount);
            }
        }
        else
        {
            if (currentRegularTargetCount > 0)
            {
                currentRegularTargetCount--;
                Debug.Log("Regular Target Despawned. Count: " + currentRegularTargetCount);
            }
        }
    }
}