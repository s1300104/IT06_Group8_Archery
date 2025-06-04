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

    [Header("Look At Player Settings")]
    [Tooltip("ターゲットが常にプレイヤーの方を向くようにするかどうか")]
    public bool alwaysFacePlayer = true; // インスペクターでこの機能をON/OFFできるようにする
    [Tooltip("プレイヤーの方を向く際の回転速度（0の場合は即時回転）")]
    public float facePlayerRotationSpeed = 5.0f;
    private Transform playerToFace; // プレイヤーのTransformへの参照

    private Vector3 initialWorldMovementDirection;

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

        // プレイヤー参照の取得を共通化
        if (alwaysFacePlayer || pattern == MovementPattern.SlowChasePlayer) // プレイヤーを向く機能がON、または追跡タイプの場合
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerToFace = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("TargetMovement: Player object with tag 'Player' not found. 'Always Face Player' and 'SlowChasePlayer' might not work correctly.");
                if (pattern == MovementPattern.SlowChasePlayer) // SlowChasePlayerでPlayerが見つからない場合はフォールバック
                {
                     this.currentPattern = MovementPattern.Straight; // Straightにフォールバック [cite: 124, 519, 783, 856]
                }
            }
        }

        // パターンごとの初期設定
        switch (currentPattern)
        {
            case MovementPattern.Straight:
                transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0); 
                initialWorldMovementDirection = transform.forward; // ★初期のワールド前方方向を保存
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
                initialWorldMovementDirection = transform.forward; // ★初期のワールド前方方向を保存
                break;
            case MovementPattern.SineWaveHorizontal:
            case MovementPattern.SineWaveVertical:
                var dir = Random.insideUnitCircle.normalized;
                Vector3 calculatedBaseDirection = new Vector3(dir.x, pattern == MovementPattern.SineWaveVertical ? dir.y : 0, pattern == MovementPattern.SineWaveHorizontal ? dir.y : 0).normalized; // 正規化
                if (calculatedBaseDirection == Vector3.zero) calculatedBaseDirection = Vector3.forward; // ゼロベクトル対策
                initialWorldMovementDirection = calculatedBaseDirection; // ★初期のワールド移動方向を保存
                transform.rotation = Quaternion.LookRotation(initialWorldMovementDirection, Vector3.up); 
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

        if (alwaysFacePlayer && playerToFace != null)
        {
            // 特定のパターンでは独自の向き制御を優先、または向きを変えない
            bool applyFacingLogic = true;
            // switch (currentPattern)
            // {
            //     case MovementPattern.SlowChasePlayer: // 独自のLookAtを持つ
            //     // case MovementPattern.Circular:     // 独自の向き制御があり得る
            //     // case MovementPattern.HorizontalOscillation: // 向き固定のことが多い
            //     // case MovementPattern.VerticalOscillation:   // 向き固定のことが多い
            //         applyFacingLogic = false;
            //         break;
            //     default:
            //         break;
            // }

            if (applyFacingLogic)
            {
                Vector3 directionToPlayer = playerToFace.position - transform.position;
                directionToPlayer.y = 0;
                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(-directionToPlayer);
                    if (facePlayerRotationSpeed > 0)
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, facePlayerRotationSpeed * Time.deltaTime);
                    else
                        transform.rotation = targetRotation;
                }
            }
        }

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
                    // StopAndGoの場合、移動方向はStopAndGoRoutineInternalで設定された向きを維持
                    // alwaysFacePlayerがONでも、移動方向自体は変わらない
                    transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
                }
                break;
            case MovementPattern.RandomWaypoint:
                // コルーチンで処理 向きはWaypointへの方向
                break;
        }
    }
    
    void MoveStraight()
    {
        // ★保存された初期ワールド移動方向に沿って移動
        transform.Translate(initialWorldMovementDirection * speed * Time.deltaTime, Space.World);
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
        Vector3 right = Quaternion.LookRotation(initialWorldMovementDirection, Vector3.up) * Vector3.right;
        Vector3 up = Quaternion.LookRotation(initialWorldMovementDirection, Vector3.up) * Vector3.up;
        Vector3 offset = right * x + up * y;
        transform.position = circleCenter + offset;
    }

    void MoveSineWaveHorizontal()
    {
        // ★保存された初期ワールド移動方向に沿って線形移動
        transform.Translate(initialWorldMovementDirection * speed * Time.deltaTime, Space.World);

        float sineOffset = Mathf.Sin(timeAccumulator * waveFrequency) * waveAmplitude; // [cite: 149, 284, 548, 621]

        // 揺れは initialWorldMovementDirection に対して水平垂直な軸で行う
        Vector3 horizontalPerpendicular = Vector3.Cross(initialWorldMovementDirection, Vector3.up).normalized;
        if (horizontalPerpendicular == Vector3.zero) // initialWorldMovementDirection がY軸に平行な場合
        {
            horizontalPerpendicular = transform.right; // フォールバックとして現在の右方向（プレイヤー基軸）
        }
        transform.position += horizontalPerpendicular * sineOffset * Time.deltaTime;
    }

    void MoveSineWaveVertical()
    {
        // ★保存された初期ワールド移動方向に沿って線形移動
        transform.Translate(initialWorldMovementDirection * speed * Time.deltaTime, Space.World);

        float sineOffset = Mathf.Sin(timeAccumulator * waveFrequency) * waveAmplitude; // [cite: 152, 287, 551, 624]

        // 揺れは initialWorldMovementDirection に対して垂直な面での「上」方向
        // (initialWorldMovementDirectionが水平に近い場合、これはほぼワールドのY軸になる)
        // より厳密な「パスに対する垂直上」を求めるなら:
        Vector3 sideAxis = Vector3.Cross(initialWorldMovementDirection, Vector3.up).normalized;
        if (sideAxis == Vector3.zero) sideAxis = Vector3.right; // フォールバック
        Vector3 verticalPerpendicular = Vector3.Cross(initialWorldMovementDirection, sideAxis).normalized; // initialWorldMovementDirectionのXY平面での回転軸から求める
         if (verticalPerpendicular == Vector3.zero || float.IsNaN(verticalPerpendicular.x)) verticalPerpendicular = Vector3.up; // 万が一のフォールバック

        transform.position += verticalPerpendicular * sineOffset * Time.deltaTime;
    }

    void MoveSlowChasePlayer()
    {
        if (playerTransform != null)
        {
            // Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            // if (directionToPlayer != Vector3.zero)
            // {
            //     Quaternion targetRotation = Quaternion.LookRotation(-directionToPlayer);
            //     transform.rotation = targetRotation;
            // }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer > minChaseDistance)
            {
                // transform.LookAt(playerTransform.position);
                transform.Translate(-Vector3.forward * chaseSpeed * Time.deltaTime);
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