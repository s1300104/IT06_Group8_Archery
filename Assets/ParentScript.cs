using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MoveParent(Vector3 deltaPosition)
    {
        transform.localPosition = deltaPosition;
        // Debug.Log("親オブジェクトの座標をParentScriptから更新しました: " + transform.localPosition);
    }
}
