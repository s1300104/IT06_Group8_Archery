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

    [Header("Spawn Restriction Settings")]
    [Tooltip("中央禁止領域の中心の、spawnAreaCenterからの相対オフセット")]
    public Vector3 exclusionZoneOffset = Vector3.zero; // 通常は(0,0,0)でspawnAreaCenterと中心を共有

    [Tooltip("中央禁止領域のサイズ (XYZ)")]
    public Vector3 exclusionZoneSize = Vector3.zero; // サイズが0なら禁止領域なしとして扱う

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
        Vector3 spawnPosition;

        bool positionIsValid = TryFindValidSpawnPosition(out spawnPosition);

        if (!positionIsValid)
        {
            Debug.LogWarning("TargetSpawner: 有効なスポーン位置が見つかりませんでした。今回のスポーンは見送ります。");
            return; // 有効な位置が見つからなければスポーンしない
        }

        if (isBonus)
        {
            BonusTarget bonusTarget = TargetPoolManager.Instance.SpawnBonusTarget(spawnPosition);
            if (bonusTarget != null)
            {
                currentBonusTargetCount++;
                // Debug.Log("Bonus Target Spawned. Count: " + currentBonusTargetCount);
            }
        }
        else
        {
            PooledTarget regularTarget = TargetPoolManager.Instance.SpawnRegularTarget(spawnPosition);
            if (regularTarget != null)
            {
                currentRegularTargetCount++;
                // Debug.Log("Regular Target Spawned. Count: " + currentRegularTargetCount);
            }
        }
    }

    bool TryFindValidSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        int maxSpawnRetries = 20; // 有効な位置を見つけるまでの最大試行回数

        for (int i = 0; i < maxSpawnRetries; i++)
        {
            // 1. スポーン範囲内でランダムな座標を生成
            float rawX = Random.Range(spawnAreaCenter.x - spawnAreaSize.x / 2f, spawnAreaCenter.x + spawnAreaSize.x / 2f);
            // 2. Y座標は spawnAreaCenter.y から 上限までの範囲に限定 (上半球を意識)
            float restrictedY = Random.Range(spawnAreaCenter.y, spawnAreaCenter.y + spawnAreaSize.y / 2f);
            float rawZ = Random.Range(spawnAreaCenter.z - spawnAreaSize.z / 2f, spawnAreaCenter.z + spawnAreaSize.z / 2f);

            Vector3 currentAttemptPosition = new Vector3(rawX, restrictedY, rawZ);

            // 3. 禁止領域のチェック (exclusionZoneSizeのいずれかの要素が0より大きい場合のみ)
            bool isInExclusionZone = false;
            if (exclusionZoneSize.x > 0 || exclusionZoneSize.y > 0 || exclusionZoneSize.z > 0)
            {
                Vector3 actualExclusionCenter = spawnAreaCenter + exclusionZoneOffset;
                Vector3 exclusionMin = actualExclusionCenter - exclusionZoneSize / 2f;
                Vector3 exclusionMax = actualExclusionCenter + exclusionZoneSize / 2f;

                bool isInExclusionX = currentAttemptPosition.x >= exclusionMin.x && currentAttemptPosition.x <= exclusionMax.x;
                bool isInExclusionY = currentAttemptPosition.y >= exclusionMin.y && currentAttemptPosition.y <= exclusionMax.y;
                bool isInExclusionZ = currentAttemptPosition.z >= exclusionMin.z && currentAttemptPosition.z <= exclusionMax.z;

                if (isInExclusionX && isInExclusionY && isInExclusionZ)
                {
                    isInExclusionZone = true;
                }
            }

            if (isInExclusionZone)
            {
                continue; // 禁止領域内なので再試行
            }

            // すべての条件を満たしたので、この位置は有効
            spawnPosition = currentAttemptPosition;
            return true; // 有効な位置が見つかった
        }

        return false; // 最大試行回数以内に有効な位置が見つからなかった
    }

    // PooledTarget (およびそれを継承するBonusTarget) のOnDespawnから呼び出される
    // 引数としてデスポーンしたターゲットのインスタンスを受け取る
    public void OnTargetDespawned(PooledTarget despawnedTarget)
    {
        Vector3 spawnPosition;
        bool positionIsValid = TryFindValidSpawnPosition(out spawnPosition);

        if (positionIsValid)
        {
            despawnedTarget.UpdatePosition(spawnPosition);
        }

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

    void OnDrawGizmosSelected()
    {
        // 全体のスポーン範囲
        Gizmos.color = new Color(0, 1, 0, 0.3f); // 緑色で半透明
        Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);

        // 上半球のみの範囲 (簡易的に元の範囲の上半分を示す)
        Gizmos.color = new Color(0, 0, 1, 0.3f); // 青色で半透明
        Vector3 upperHalfCenter = new Vector3(spawnAreaCenter.x, spawnAreaCenter.y + spawnAreaSize.y / 4f, spawnAreaCenter.z);
        Vector3 upperHalfSize = new Vector3(spawnAreaSize.x, spawnAreaSize.y / 2f, spawnAreaSize.z);
        Gizmos.DrawCube(upperHalfCenter, upperHalfSize);

        // 禁止領域 (exclusionZoneSizeが0より大きい場合のみ)
        if (exclusionZoneSize.x > 0 || exclusionZoneSize.y > 0 || exclusionZoneSize.z > 0)
        {
            Gizmos.color = new Color(1, 0, 0, 0.4f); // 赤色で半透明
            Gizmos.DrawCube(spawnAreaCenter + exclusionZoneOffset, exclusionZoneSize);
        }
    }
}