using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    /*
    public Transform nockingPoint; // 弓の弦の初期位置
    public Transform pullingHand; // 矢を引いている手のコントローラー
    */
    public float maxPullDistance = 0.5f; // 最大まで引ける距離 (メートル)
    private float currentPullAmount = 0f; // 現在の引いている割合 (0.0 ~ 1.0)
    // Update is called once per frame
    void Update()
    {
        if (isArrowNocked && pullingHand != null) // 矢がセットされ、引く手が認識されている場合
        {
            float distance = Vector3.Distance(nockingPoint.position, pullingHand.position);
            currentPullAmount = Mathf.Clamp01(distance / maxPullDistance);
            // currentPullAmount を使って弓のしなりや発射力を制御
        }
        else
        {
            currentPullAmount = 0f;
        }
    }
}
