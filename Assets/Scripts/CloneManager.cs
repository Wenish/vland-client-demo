//

using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class CloneManager : MonoBehaviour
{

#if UNITY_EDITOR
    public bool isAutoStart;
    public Mirror.NetworkManager _networkManager;
    // Start is called before the first frame update
    void Start()
    {
        if (!isAutoStart) return;

        if (ClonesManager.IsClone())
        {
            _networkManager.StartClient();
        }
        else
        {
            _networkManager.StartHost();
        }
    }
#endif
}