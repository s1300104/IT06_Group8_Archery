using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WBStringController : MonoBehaviour
{
    Vector3 StringGrabGpos;     // 弦を持つ手のグローバル位置を格納
    Vector3 InitStringLpos;     // 弦の初期ローカル位置を格納 
    Vector3 StringGpos;
    Vector3 InitHnadGrabStringLpos;
    Vector3 BowGrabGpos;        // 弓の持ち手のグローバル位置を格納
    Vector3 BowGrabLpos;        // 弓の持ち手のローカル位置を格納
    Transform arrowSpawnPoint;
    Vector3 shootDirection;
    float minForce = 10f;       // 矢の最低速度
    float maxForce = 50f;       // 矢の最高速度
    float maxDrawDistance = -0.07f;// 弦の最長の引き
    bool StringPulled = false;
    private XRSocketInteractor socketInteractor;
    private XRGrabInteractable stringGrabInteractable;
    private XRGrabInteractable bowGrabInteractable;
    private bool isGrabbing = false;
    private ParentScript parentScript;
    private GameObject currentArrow;
    private Rigidbody currentArrowRigidbody;
    private BoxCollider currentArrowCollider;
    private Vector3 ArrowColliderSize;
    private Vector3 ArrowColliderCenter;
    // 弓と矢の衝突判定
    public GameObject _colA;
    public GameObject _colB;
    public GameObject _colC;
    public bool ignoreCollision;
    [Header("放物線ガイド設定")]
    [SerializeField] private LineRenderer trajectoryLineRenderer; // Line Rendererコンポーネンスをアタッチ
    [SerializeField] private int trajectoryPointCount = 50;      // 軌道上の点の数
    [SerializeField] private float trajectoryTimeStep = 0.05f;   // 軌道計算のタイムステップ (秒)
    [SerializeField] private float maxPredictionTime = 3f;       // 最大予測時間 (秒)
    [Header("サウンド設定")]
    [SerializeField] private AudioSource bowAudioSource; // 弓にアタッチするAudioSource
    [SerializeField] private AudioClip bowDrawSound;     // 弓を引く音
    [SerializeField] private AudioClip arrowNockSound;   // 矢を番える音
    [SerializeField] private AudioClip bowReleaseSound;  // 矢を放つ音

    
    // Start is called before the first frame update
    void Start()
    {
        
        InitStringLpos = GameObject.Find("WB.string").transform.localPosition;
        InitHnadGrabStringLpos = GameObject.Find("Attach_string").transform.localPosition;
        stringGrabInteractable = GetComponent<XRGrabInteractable>();
        bowGrabInteractable = GameObject.Find("Wooden Bow").GetComponent<XRGrabInteractable>();

        // 親オブジェクトにアタッチされているParentScriptのインスタンスを取得
        parentScript = GetComponentInParent<ParentScript>();
        if (parentScript == null)
        {
            Debug.LogWarning("親オブジェクトにParentScriptが見つかりませんでした。");
        }
        SpawnNewArrow();

    }

    // Update is called once per frame
    void Update()
    {
        
        if(bowGrabInteractable.isSelected)
        {
            currentArrowCollider.size = new Vector3(ArrowColliderSize.x, ArrowColliderSize.y / 2.0f, ArrowColliderSize.z);
            currentArrowCollider.center = new Vector3(ArrowColliderCenter.x, ArrowColliderCenter.y * 2.0f, ArrowColliderCenter.z);
        }
        else
        {
            currentArrowCollider.size = ArrowColliderSize;
            currentArrowCollider.center = ArrowColliderCenter;
        }                
        if (stringGrabInteractable.isSelected)
        {
            isGrabbing = true;
            //Debug.Log("オブジェクトが掴まれています！");
        }
        else
        {
            isGrabbing = false;
            // Debug.Log("オブジェクトは掴まれていません。");
        }
        
        if (parentScript != null)
        {
            StringGpos = GameObject.Find("WB.string").transform.position;
            StringGrabGpos = GameObject.Find("Attach_string").transform.position;
            BowGrabGpos = GameObject.Find("Attach_bow").transform.position;
            BowGrabLpos = GameObject.Find("Attach_bow").transform.localPosition;
            
            float newY = Vector3.Dot(StringGrabGpos-BowGrabGpos, new Vector3(0.0f, BowGrabLpos.y, 0.0f));
            if(newY >= -0.01f)newY = -0.01f;
            if(newY <= -0.07f)newY = -0.07f;
            // 射出力を計算 (引き距離に応じて線形補間)
            float forceMagnitude = Mathf.Lerp(minForce, maxForce, newY / maxDrawDistance);
            // 矢を放つ方向 
            shootDirection = BowGrabGpos - StringGpos;
            shootDirection.Normalize(); // 方向ベクトルを正規化
            // 矢の初期速度ベクトルを計算
            Vector3 initialVelocity = shootDirection * forceMagnitude;

            if(isGrabbing)
            {        
                // 弦を引く距離が一定以上になったら音を再生
                if (newY <= -0.03f && StringPulled) { 
                    bowAudioSource.PlayOneShot(bowDrawSound); 
                    Debug.Log("pull arrow sound");
                    StringPulled = false;
                }
                if(newY > -0.03f){StringPulled = true;}
                // 軌道ガイドの更新
                UpdateTrajectoryGuide(BowGrabGpos, initialVelocity);
                trajectoryLineRenderer.enabled = true; // ガイドを表示
                // ParentScriptの公開メソッドを呼び出して親の座標を変更
                parentScript.MoveParent(new Vector3(0.0f, newY, 0.0f));
            }
            else if(newY <= -0.03f)
            {
                
                ShootArrow(newY);

                transform.localPosition = InitHnadGrabStringLpos;
                parentScript.MoveParent(InitStringLpos);
                Invoke("SpawnNewArrow", 0.5f);
                trajectoryLineRenderer.enabled = false; // ガイドを非表示
            }
            else
            {
                StringPulled = true;
                transform.localPosition = InitHnadGrabStringLpos;
                parentScript.MoveParent(InitStringLpos);
                trajectoryLineRenderer.enabled = false; // ガイドを非表示
            }
        }   
    }


    void SpawnNewArrow()
    {
        
        GameObject arrowPrefab = (GameObject)Resources.Load("Arrow_stick");
        arrowSpawnPoint = GameObject.Find("Arrow_nocking_point").transform;
        Debug.Log(arrowSpawnPoint.rotation);
        if(arrowPrefab != null && arrowSpawnPoint != null)
        {
            currentArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowSpawnPoint.rotation);
            currentArrowCollider = currentArrow.GetComponent<BoxCollider>();
            ArrowColliderSize = new Vector3(currentArrowCollider.size.x, currentArrowCollider.size.y, currentArrowCollider.size.z);
            ArrowColliderCenter = new Vector3(currentArrowCollider.center.x, currentArrowCollider.center.y, currentArrowCollider.center.z);
            currentArrowRigidbody = currentArrow.GetComponent<Rigidbody>();
            if(currentArrowRigidbody != null)
            {
                currentArrowRigidbody.isKinematic = true; // 最初は物理演算を無効化
                IgnoreCollider();
                bowAudioSource.PlayOneShot(arrowNockSound);
            }
            else
            {
                Debug.Log("矢のプレハブにRigidbodyがありません！");
            }
        }
        else
        {
            Debug.Log("矢のプレハブまたは矢のスポーンポイントが設定されていません！");
        }
    }

    void ShootArrow(float drawDistance)
    {
        
        socketInteractor = GameObject.Find("Arrow_nocking_point").GetComponent<XRSocketInteractor>();
        IgnoreSocket();
        if (currentArrow == null || currentArrowRigidbody == null)
        {
            Debug.LogError("矢が準備されていません！");
            Invoke("EnableSocket", 0.5f);
            return;
        }

        // 射出力を計算 (引き距離に応じて線形補間)
        float forceMagnitude = Mathf.Lerp(minForce, maxForce, drawDistance / maxDrawDistance);

        StringGpos = GameObject.Find("WB.string").transform.position;
        BowGrabGpos = GameObject.Find("Attach_bow").transform.position;
        // 矢を放つ方向 
        shootDirection = BowGrabGpos - StringGpos;
        shootDirection.Normalize(); // 方向ベクトルを正規化

        // 物理演算を有効化
        currentArrowRigidbody.isKinematic = false;
        // 重力を有効化
        currentArrowRigidbody.useGravity = true;
        // IsTriggerを有効化
        BoxCollider currentArrowCollider = currentArrow.GetComponent<BoxCollider>();
        currentArrowCollider.isTrigger = true;
        // 矢に力を加える
        currentArrowRigidbody.AddForce(shootDirection * forceMagnitude, ForceMode.VelocityChange); // VelocityChangeは即座に速度を変化させる
        
        // 矢を放った後の処理 (例: 弦の音を再生)
        bowAudioSource.PlayOneShot(bowReleaseSound);
        Invoke("EnableSocket", 0.5f);
    }

    private void UpdateTrajectoryGuide(Vector3 startPosition, Vector3 initialVelocity)
    {
        // 軌道上の点を格納するリスト
        List<Vector3> points = new List<Vector3>();
        points.Add(startPosition); // 開始点を追加

        Vector3 currentPosition = startPosition;
        Vector3 currentVelocity = initialVelocity; // 現在の速度
        float currentTime = 0f;

        // 重力加速度 (UnityのPhysics.gravityを使用)
        Vector3 gravity = Physics.gravity;

        for (int i = 0; i < trajectoryPointCount; i++)
        {
            // 次の予測点の時間を計算
            currentTime += trajectoryTimeStep;

            // 等加速度運動の公式 (x = x0 + v0*t + 0.5*a*t^2)
            // この式は、空気抵抗を無視したシンプルな放物線計算です。
            Vector3 predictedPosition = startPosition + (initialVelocity * currentTime) + (0.5f * gravity * currentTime * currentTime);

            points.Add(predictedPosition);

            // 最大予測時間を超えたらループを終了
            if (currentTime >= maxPredictionTime)
            {
                break;
            }

            // オプション: 軌道が地面に衝突したかをチェックし、衝突したらそこで軌道を打ち切る
            // Raycastを使用するとより正確ですが、ここではパフォーマンスのため簡易的に。
            // LayerMaskを使って、地面や壁のみを検出するようにすると良いでしょう。
            // RaycastHit hit;
            // if (Physics.Raycast(currentPosition, (predictedPosition - currentPosition).normalized, out hit, Vector3.Distance(currentPosition, predictedPosition), obstacleLayer))
            // {
            //     points.Add(hit.point);
            //     break;
            // }
            currentPosition = predictedPosition;
        }

        // Line Rendererに点を設定
        trajectoryLineRenderer.positionCount = points.Count;
        trajectoryLineRenderer.SetPositions(points.ToArray());
    }

    void IgnoreCollider()
    {
        
        ignoreCollision = true;
        // 弓と矢の衝突判定を無効化
        Physics.IgnoreCollision(_colA.GetComponent<Collider>(), currentArrow.GetComponent<Collider>(), ignoreCollision);
        Physics.IgnoreCollision(_colC.GetComponent<Collider>(), currentArrow.GetComponent<Collider>(), ignoreCollision);
    }
    void EnableCollider()
    {
        ignoreCollision = false;
        // 弓と矢の衝突判定を有効化
        Physics.IgnoreCollision(_colA.GetComponent<Collider>(), currentArrow.GetComponent<Collider>(), ignoreCollision);
        Physics.IgnoreCollision(_colC.GetComponent<Collider>(), currentArrow.GetComponent<Collider>(), ignoreCollision);
    }
    void IgnoreSocket()
    {
        socketInteractor.enabled = false;
    }
    void EnableSocket()
    {
        socketInteractor.enabled = true;
    }
}
