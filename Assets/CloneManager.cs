//
#if UNITY_EDITOR
using UnityEngine;
using ParrelSync;

public class CloneManager : MonoBehaviour
{
    public bool isAutoStart;
    public CustomNetworkManager _networkManager;
    // Start is called before the first frame update
    void Start()
    {
        if (!isAutoStart) return;

        if (ClonesManager.IsClone()) {
            _networkManager.StartClient();
        } else {
            _networkManager.StartHost();
        }
    }
}
#endif