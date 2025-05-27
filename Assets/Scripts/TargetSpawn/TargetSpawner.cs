using UnityEngine;
using System.Collections;

public class TargetSpawner : MonoBehaviour
{
    public float spawnInterval = 2.0f;
    public int maxTargets = 10;
    public Vector3 spawnAreaCenter;
    public Vector3 spawnAreaSize;

    private int currentTargetCount = 0;

    void Start()
    {
        StartCoroutine(SpawnTargetCoroutine());
    }

    IEnumerator SpawnTargetCoroutine()
    {
        while (true)
        {
            if (currentTargetCount < maxTargets)
            {
                Spawn();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void Spawn()
    {
        // ランダム位置計算
        var x = Random.Range(spawnAreaCenter.x - spawnAreaSize.x/2, spawnAreaCenter.x + spawnAreaSize.x/2);
        var y = Random.Range(spawnAreaCenter.y - spawnAreaSize.y/2, spawnAreaCenter.y + spawnAreaSize.y/2);
        var z = Random.Range(spawnAreaCenter.z - spawnAreaSize.z/2, spawnAreaCenter.z + spawnAreaSize.z/2);
        var pos = new Vector3(x,y,z);

        TargetPoolManager.Instance.SpawnTarget(pos);
        currentTargetCount++;
    }

    // ターゲットが寿命切れで返却された際に呼ばれる
    public void OnTargetDespawned()
    {
        if (currentTargetCount > 0) currentTargetCount--;
    }
}

/*
※PooledTarget の OnDespawn 内で:
    TargetSpawner sp = FindObjectOfType<TargetSpawner>();
    sp.OnTargetDespawned();
で currentTargetCount をデクリメントしてください。
*/
