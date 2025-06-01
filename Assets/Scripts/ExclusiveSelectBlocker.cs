using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering; // IXRSelectFilterのため

public class ExclusiveSelectBlocker : MonoBehaviour, IXRSelectFilter
{
    public bool canProcess => isActiveAndEnabled;
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactable is XRBaseInteractable baseInteractable)
        {
            bool isAlreadyGrabbedByOther =
                baseInteractable.interactorsSelecting.Count > 0
                && !baseInteractable.interactorsSelecting.Contains(interactor);
            if (isAlreadyGrabbedByOther)
                return false; // 選択を拒否
        }
        return true; // 選択を許可
    }
}
