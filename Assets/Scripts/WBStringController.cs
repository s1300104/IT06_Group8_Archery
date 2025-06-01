using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WBStringController : MonoBehaviour
{
    //Vector3 ArrowPos;  // 矢の尾羽の位置を格納
    Vector3 BowHandGpos;
    Vector3 BowGrabGpos;         // 弓の持ち手の位置を格納
    Vector3 BowGrabLpos;
    private XRGrabInteractable grabInteractable;
    private bool isGrabbing = false;
    private ParentScript parentScript;
    //Vector3 BowStringPos;   // 弓の弦の位置を格納
    //Vector3 BowTopPos;      // 弓の末弭の位置を格納
    //Vector3 BowButtomPos;   // 弓の本弭の位置を格納
    // Start is called before the first frame update
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // 親オブジェクトにアタッチされているParentScriptのインスタンスを取得
        parentScript = GetComponentInParent<ParentScript>();
        if (parentScript == null)
        {
            Debug.LogWarning("親オブジェクトにParentScriptが見つかりませんでした。");
        }
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
        
        if(isGrabbing)
        {
            if (parentScript != null)
            {
                BowHandGpos = GameObject.Find("Attach_string").transform.position;
                BowGrabGpos = GameObject.Find("Attach_bow").transform.position;
                BowGrabLpos = GameObject.Find("Attach_bow").transform.localPosition;

                float newY = Vector3.Dot(BowHandGpos-BowGrabGpos, new Vector3(0.0f, BowGrabLpos.y, 0.0f));

                //transform.localPosition = ;
                // ParentScriptの公開メソッドを呼び出して親の座標を変更
                parentScript.MoveParent(new Vector3(0.0f, newY, 0.0f));
            }
        }
        // 掴んでいる間の処理
        //if (isGrabbing)    
        //{
            
        //}
        /*
        ArrowPos = GameObject.Find("Attach_arrow").transform.localPosition;
        BowGrabPos = GameObject.Find("Attach_bow").transform.localPosition;
        BowStringPos = GameObject.Find("Attach_string").transform.localPosition;
        BowTopPos = GameObject.Find("WB.top.horn").transform.localPosition;
        BowButtomPos = GameObject.Find("WB.down.horn").transform.localPosition;

        Debug.Log("弓 : " + BowGrabPos.x + ", " + BowGrabPos.y + ", "+ BowGrabPos.z );
        Debug.Log("矢 : " + ArrowPos.x + ", " + ArrowPos.y + ", "+ ArrowPos.z );

        if(Vector3.Dot(BowStringPos-BowGrabPos, Vector3.Cross(BowTopPos-BowGrabPos, BowButtomPos-BowGrabPos)) != 0)
        {
            transform.localPosition = BowStringPos;
            //transform.localPosition = new Vector3(BowStringPos.x, 0.0f, 0.0f);
        }
        */
    }
}
