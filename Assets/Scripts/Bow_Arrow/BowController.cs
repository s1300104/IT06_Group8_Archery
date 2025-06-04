using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BowController : XRGrabInteractable
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        // 既に他のインタラクターによって選択されているか（保持されているか）どうかを確認
        // interactorsSelecting リストには、現在このオブジェクトを選択しているインタラクターが含まれる
        bool isAlreadyGrabbedByOther = interactorsSelecting.Count > 0 && !interactorsSelecting.Contains(interactor);
    
        if (isAlreadyGrabbedByOther)
        {
            // 既に他の手で掴まれている場合、このインタラクター（別の手）による選択は許可しない
            return false;
        }
    
        // 上記以外の場合（誰も掴んでいない、または掴もうとしているのが現在掴んでいる手自身であるなど）、
        // 基底クラスの選択ロジック（距離やレイヤーマスクなど）に従う
        return base.IsSelectableBy(interactor);
    }
}
