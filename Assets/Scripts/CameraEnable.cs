using UnityEngine;

namespace raspberly.ovr
{
    public class CameraEnable : MonoBehaviour
    {
        void Start ()
        {
            GetComponent<Camera>().enabled = false;
            GetComponent<Camera>().enabled = true;
        }
    }
}