using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class GateController : NetworkBehaviour
{
    [SerializeField]
    private Collider gateCollider;

    [SerializeField]
    private NavMeshObstacle navMeshObstacle;

    [SerializeField]
    private GameObject gateObject;

    [SerializeField]
    [SyncVar(hook = nameof(OnIsOpenChanged))]
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    [Server]
    public void OpenGate()
    {
        if (isOpen) return;
        // Open the gate
        ChangeGateState(true);
    }

    [Server]
    public void CloseGate()
    {
        if (!isOpen) return;
        // Close the gate
        ChangeGateState(false);
    }

    public void ChangeGateState(bool isOpen) {
        this.isOpen = isOpen;
        gateCollider.enabled = !isOpen;
        navMeshObstacle.enabled = !isOpen;
        gateObject.SetActive(!isOpen);
    }

    private void OnIsOpenChanged(bool oldValue, bool newValue)
    {
        ChangeGateState(newValue);
    }

    #if UNITY_EDITOR
        [ContextMenu("Close Gate")]
        private void CloseGateFromEditor()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Cannot close gate outside of play mode.");
                return;
            }

            if (isServer)
            {
                CloseGate();
            }
            else
            {
                Debug.LogWarning("CloseGate can only be called on the server.");
            }
        }
    #endif
    

    #if UNITY_EDITOR
        [ContextMenu("Open Gate")]
        private void OpenGateFromEditor()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Cannot open gate outside of play mode.");
                return;
            }

            if (isServer)
            {
                OpenGate();
            }
            else
            {
                Debug.LogWarning("OpenGate can only be called on the server.");
            }
        }
    #endif
}