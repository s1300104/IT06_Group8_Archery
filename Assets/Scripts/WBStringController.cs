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
    private XRSocketInteractor socketInteractor;
    private XRGrabInteractable grabInteractable;
    private bool isGrabbing = false;
    private ParentScript parentScript;
    private GameObject currentArrow;
    private Rigidbody currentArrowRigidbody;

    
    // Start is called before the first frame update
    void Start()
    {
        
        InitStringLpos = GameObject.Find("WB.string").transform.localPosition;
        InitHnadGrabStringLpos = GameObject.Find("Attach_string").transform.localPosition;
        grabInteractable = GetComponent<XRGrabInteractable>();

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
        
        if (grabInteractable.isSelected)
        {
            isGrabbing = true;
            Debug.Log("オブジェクトが掴まれています！");
        }
        else
        {
            isGrabbing = false;
            Debug.Log("オブジェクトは掴まれていません。");
        }
        
        if (parentScript != null)
        {
            StringGrabGpos = GameObject.Find("Attach_string").transform.position;
            BowGrabGpos = GameObject.Find("Attach_bow").transform.position;
            BowGrabLpos = GameObject.Find("Attach_bow").transform.localPosition;

            float newY = Vector3.Dot(StringGrabGpos-BowGrabGpos, new Vector3(0.0f, BowGrabLpos.y, 0.0f));
            if(newY >= -0.01f)newY = -0.01f;
            if(newY <= -0.07f)newY = -0.07f;
            if(isGrabbing)
            {        
                //transform.localPosition = ;
                // ParentScriptの公開メソッドを呼び出して親の座標を変更
                parentScript.MoveParent(new Vector3(0.0f, newY, 0.0f));
            }
            else if(newY <= -0.03f)
            {
                ShootArrow(newY);

                transform.localPosition = InitHnadGrabStringLpos;
                parentScript.MoveParent(InitStringLpos);
            }
            else
            {
                transform.localPosition = InitHnadGrabStringLpos;
                parentScript.MoveParent(InitStringLpos);
            }
        }   
    }


    void SpawnNewArrow()
    {
        
        GameObject arrowPrefab = (GameObject)Resources.Load("Arrow_stick");
        Transform arrowSpawnPoint = this.transform;
        if(arrowPrefab != null && arrowSpawnPoint != null)
        {
            currentArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowSpawnPoint.rotation);
            currentArrowRigidbody = currentArrow.GetComponent<Rigidbody>();
            if(currentArrowRigidbody != null)
            {
                currentArrowRigidbody.isKinematic = true; // 最初は物理演算を無効化
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
        float minForce = 1f;
        float maxForce = 5f;
        float maxDrawDistance = -0.07f;
        socketInteractor = GameObject.Find("Arrow_nocking_point").GetComponent<XRSocketInteractor>();
        socketInteractor.enabled = false;
        /*if (currentArrow == null || currentArrowRigidbody == null)
        {
            Debug.LogError("矢が準備されていません！");
            return;
        }*/

        // 射出力を計算 (引き距離に応じて線形補間)
        float forceMagnitude = Mathf.Lerp(minForce, maxForce, drawDistance / maxDrawDistance);


        StringGpos = GameObject.Find("WB.string").transform.position;
        BowGrabGpos = GameObject.Find("Attach_bow").transform.position;
        // 矢を放つ方向 (弓のフォワード方向を基準に)
        //Vector3 worldVec = transform.up;
        //Vector3 shootDirection = transform.TransformDirection(worldVec); // 弓の向き
        //Vector3 shootDirection = transform.up;
        //Vector3 shootDirection = BowGrabGpos - StringGpos;
        // わずかに上向きの力を加えることで、現実的な放物線にする
        //shootDirection += Vector3.up * upwardForceMultiplier;

        StringGrabGpos = GameObject.Find("Attach_string").transform.position;
        BowGrabGpos = GameObject.Find("Attach_bow").transform.position;
        BowGrabLpos = GameObject.Find("Attach_bow").transform.localPosition;

        float newY = Vector3.Dot(StringGrabGpos-BowGrabGpos, new Vector3(0.0f, BowGrabLpos.y, 0.0f));
        if(newY >= -0.01f)newY = -0.01f;
        if(newY <= -0.07f)newY = -0.07f;
        Vector3 shootDirection = transform.TransformDirection(new Vector3(0.0f, newY, 0.0f));
        shootDirection.Normalize(); // 方向ベクトルを正規化

        // 物理演算を有効化
        currentArrowRigidbody.isKinematic = false;

        // 矢に力を加える
        currentArrowRigidbody.AddForce(shootDirection * forceMagnitude, ForceMode.VelocityChange); // VelocityChangeは即座に速度を変化させる

        // 矢を放った後の処理 (例: 弦の音を再生)
        // AudioManager.PlaySound("BowRelease"); // サウンドマネージャーがある場合
        //socketInteractor.enabled = true;
    }
}
