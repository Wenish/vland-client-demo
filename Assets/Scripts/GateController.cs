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
    private float moveDuration = 1f;

    [SerializeField]
    [SyncVar(hook = nameof(OnIsOpenChanged))]
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine moveCoroutine;

    private void Start()
    {
        closedPosition = gateObject.transform.position;

        // Calculate the open position by subtracting the gate's height in the local Y axis
        float height = gateObject.GetComponent<Renderer>().bounds.size.y;
        openPosition = closedPosition - new Vector3(0, height, 0);
        
        // Set the initial state of the gate
        ChangeGateState();
    }

    private void Update()
    {
        if (!isServer) return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            OpenGate();
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            CloseGate();
        }
    }

    [Server]
    public void OpenGate()
    {
        if (isOpen) return;
        isOpen = true;
        ChangeGateState();
    }

    [Server]
    public void CloseGate()
    {
        if (!isOpen) return;
        isOpen = false;
        ChangeGateState();
    }

    private void ChangeGateState()
    {
        gateCollider.enabled = !isOpen;
        navMeshObstacle.enabled = !isOpen;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        moveCoroutine = StartCoroutine(MoveGate(targetPosition));
    }

    private void OnIsOpenChanged(bool oldValue, bool newValue)
    {
        if (isServer) return;
        ChangeGateState();
    }

    private System.Collections.IEnumerator MoveGate(Vector3 targetPosition)
    {
        Vector3 startPosition = gateObject.transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            gateObject.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        gateObject.transform.position = targetPosition;
        moveCoroutine = null;
    }

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
}
