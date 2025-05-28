using UnityEngine;
using System.Collections;

public class TargetMovement : MonoBehaviour
{
    // 移動パターンの種類を定義
    public enum MovementPattern
    {
        Straight,
        HorizontalOscillation,
        VerticalOscillation,
        Circular,
        SineWaveHorizontal,
        SineWaveVertical,
        SlowChasePlayer,
        StopAndGo,
        RandomWaypoint
    }

    public MovementPattern currentPattern { get; private set; } // 現在のパターン
    
    [Header("General Settings")]
    public float speed = 3.0f;
    // public float lifetime = 10.0f; // PooledTargetが管理するので削除

    [Header("Oscillation Settings (Horizontal/Vertical)")]
    public float oscillationDistance = 5.0f;
    private Vector3 initialLocalPosition; // ローカル座標基準に変更

    [Header("Circular Motion Settings")]
    public float circleRadius = 3.0f;
    public float circleSpeedMultiplier = 1.0f;
    private Vector3 circleCenter;
    private float currentAngle = 0.0f;

    [Header("Sine Wave Settings")]
    public float waveFrequency = 1.0f;
    public float waveAmplitude = 1.0f;
    private Vector3 baseDirection;
    private float timeAccumulator = 0.0f;

    [Header("Slow Chase Player Settings")]
    public float chaseSpeed = 1.5f;
    public float minChaseDistance = 5.0f;
    private Transform playerTransform;

    [Header("Stop And Go Settings")]
    public float stopDuration = 2.0f;
    public float moveDuration = 3.0f;
    private bool isMovingForStopAndGo = false; // 変数名を変更して明確化
    private Coroutine stopAndGoCoroutine;

    [Header("Random Waypoint Settings")]
    public Vector3 waypointAreaSize = new Vector3(10,5,10); // スポーン位置からの相対範囲
    public float timeToReachWaypoint = 3.0f;
    private Vector3 targetWaypoint;
    private Vector3 waypointMovementStartPos;
    private float waypointTimer;
    private Coroutine randomWaypointCoroutine;


    // 外部から呼び出される初期化メソッド
    public void InitializeMovement(MovementPattern pattern, Vector3 spawnPosition)
    {
        this.currentPattern = pattern;
        transform.position = spawnPosition; // 位置を設定
        this.initialLocalPosition = transform.localPosition; // オシレーション用にローカル位置を記憶
        this.timeAccumulator = 0f; // サイン波用タイマーリセット

        // 既存の移動関連コルーチンを停止
        StopAllMovementCoroutines();

        // パターンごとの初期設定
        switch (currentPattern)
        {
            case MovementPattern.Straight:
                transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0); // ランダムな向きに
                break;
            case MovementPattern.HorizontalOscillation:
            case MovementPattern.VerticalOscillation:
                // initialLocalPosition は設定済み
                break;
            case MovementPattern.Circular:
                // 円運動の中心をオブジェクトのスポーン位置から少しずらすなど調整可能
                // ここではスポーン位置の左側を中心とする例
                circleCenter = spawnPosition - transform.right * circleRadius;
                currentAngle = Mathf.Atan2((spawnPosition - circleCenter).y, (spawnPosition - circleCenter).x);
                break;
            case MovementPattern.SineWaveHorizontal:
            case MovementPattern.SineWaveVertical:
                baseDirection = transform.forward; // 前方を基準に進む
                break;
            case MovementPattern.SlowChasePlayer:
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
                else
                {
                    Debug.LogWarning("Player not found for SlowChasePlayer pattern. Switching to Straight.");
                    this.currentPattern = MovementPattern.Straight; // フォールバック
                }
                break;
            case MovementPattern.StopAndGo:
                stopAndGoCoroutine = StartCoroutine(StopAndGoRoutineInternal());
                break;
            case MovementPattern.RandomWaypoint:
                // waypointAreaCenter はスポーン位置を基準にする
                randomWaypointCoroutine = StartCoroutine(RandomWaypointRoutineInternal(spawnPosition));
                break;
        }
    }

    public void StopAllMovementCoroutines()
    {
        if (stopAndGoCoroutine != null)
        {
            StopCoroutine(stopAndGoCoroutine);
            stopAndGoCoroutine = null;
        }
        if (randomWaypointCoroutine != null)
        {
            StopCoroutine(randomWaypointCoroutine);
            randomWaypointCoroutine = null;
        }
        isMovingForStopAndGo = false; // StopAndGoの状態もリセット
    }


    void Update()
    {
        timeAccumulator += Time.deltaTime;

        switch (currentPattern)
        {
            case MovementPattern.Straight:
                MoveStraight();
                break;
            case MovementPattern.HorizontalOscillation:
                MoveHorizontalOscillation();
                break;
            case MovementPattern.VerticalOscillation:
                MoveVerticalOscillation();
                break;
            case MovementPattern.Circular:
                MoveCircular();
                break;
            case MovementPattern.SineWaveHorizontal:
                MoveSineWaveHorizontal();
                break;
            case MovementPattern.SineWaveVertical:
                MoveSineWaveVertical();
                break;
            case MovementPattern.SlowChasePlayer:
                MoveSlowChasePlayer();
                break;
            case MovementPattern.StopAndGo:
                if (isMovingForStopAndGo)
                {
                    transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
                }
                break;
            case MovementPattern.RandomWaypoint:
                // コルーチンで処理
                break;
        }
    }

    // --- 各移動パターンの実装 (前回とほぼ同様、一部修正) ---
    void MoveStraight()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void MoveHorizontalOscillation()
    {
        // 親からの相対位置で計算
        float offset = Mathf.PingPong(Time.time * speed, oscillationDistance * 2.0f) - oscillationDistance;
        transform.localPosition = initialLocalPosition + transform.parent.right * offset; // 親の右方向に
    }

    void MoveVerticalOscillation()
    {
        float offset = Mathf.PingPong(Time.time * speed, oscillationDistance * 2.0f) - oscillationDistance;
        transform.localPosition = initialLocalPosition + transform.parent.up * offset; // 親の上方向に
    }

    void MoveCircular()
    {
        currentAngle += speed * circleSpeedMultiplier * Time.deltaTime;
        float x = Mathf.Cos(currentAngle) * circleRadius;
        float y = Mathf.Sin(currentAngle) * circleRadius;
        // circleCenter を基準に、ターゲットのローカルXY平面ではなくワールドXZ平面などで円運動させる場合
        // transform.position = circleCenter + new Vector3(x, 0, y);
        // 現在はスポーン時のターゲットの向きを基準とした円運動
        Vector3 offset = transform.right * x + transform.up * y;
        transform.position = circleCenter + offset;
    }

    void MoveSineWaveHorizontal()
    {
        transform.Translate(baseDirection * speed * Time.deltaTime, Space.World);
        float sineOffset = Mathf.Sin(timeAccumulator * waveFrequency) * waveAmplitude;
        // Y軸周りの回転から右方向を取得して揺らす
        Vector3 horizontalWiggleDirection = Quaternion.AngleAxis(90, transform.up) * baseDirection;
        transform.position += horizontalWiggleDirection.normalized * sineOffset * Time.deltaTime;

    }

    void MoveSineWaveVertical()
    {
        transform.Translate(baseDirection * speed * Time.deltaTime, Space.World);
        float sineOffset = Mathf.Sin(timeAccumulator * waveFrequency) * waveAmplitude;
        // X軸周りの回転から上方向を取得して揺らす
        Vector3 verticalWiggleDirection = Quaternion.AngleAxis(90, transform.right) * baseDirection;
        transform.position += verticalWiggleDirection.normalized * sineOffset * Time.deltaTime;
    }

    void MoveSlowChasePlayer()
    {
        if (playerTransform != null)
        {
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer > minChaseDistance)
            {
                transform.LookAt(playerTransform.position);
                transform.Translate(Vector3.forward * chaseSpeed * Time.deltaTime);
            }
        }
    }

    IEnumerator StopAndGoRoutineInternal()
    {
        while (true)
        {
            isMovingForStopAndGo = false;
            yield return new WaitForSeconds(stopDuration);

            isMovingForStopAndGo = true;
            transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0); // 移動開始時にランダムな方向を向く
            yield return new WaitForSeconds(moveDuration);
        }
    }

    IEnumerator RandomWaypointRoutineInternal(Vector3 currentSpawnPos)
    {
        waypointMovementStartPos = currentSpawnPos;
        while(true)
        {
            float randomX = Random.Range(-waypointAreaSize.x / 2, waypointAreaSize.x / 2);
            float randomY = Random.Range(-waypointAreaSize.y / 2, waypointAreaSize.y / 2);
            float randomZ = Random.Range(-waypointAreaSize.z / 2, waypointAreaSize.z / 2);
            // スポーン位置を基準とした相対座標で目標を設定
            targetWaypoint = currentSpawnPos + new Vector3(randomX, randomY, randomZ);
            waypointTimer = 0f;

            float currentMoveTime = 0f;
            while(currentMoveTime < timeToReachWaypoint)
            {
                currentMoveTime += Time.deltaTime;
                float percentageComplete = currentMoveTime / timeToReachWaypoint;
                transform.position = Vector3.Lerp(waypointMovementStartPos, targetWaypoint, percentageComplete);
                yield return null;
            }
            waypointMovementStartPos = targetWaypoint; // 次の開始位置を更新
        }
    }
}