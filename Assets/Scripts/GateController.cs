using Mirror;
using MyGame.Events;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class GateController : NetworkBehaviour
{
    public int gateId;

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
        if (gateObject == null)
        {
            Debug.LogWarning($"[{nameof(GateController)}] gateObject is not assigned. Disabling component.");
            enabled = false;
            return;
        }

        closedPosition = gateObject.transform.position;

        // Calculate the open position by subtracting the gate's height in the local Y axis
        var renderer = gateObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            float height = renderer.bounds.size.y;
            openPosition = closedPosition - new Vector3(0, height, 0);
        }
        else
        {
            // Fallback in case no renderer is present
            openPosition = closedPosition - new Vector3(0, 2f, 0);
        }

        // Set the initial state of the gate
        ChangeGateState();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Subscribe to events when running on the server
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Subscribe<OpenGateEvent>(OnOpenGateEvent);
            EventManager.Instance.Subscribe<CloseGateEvent>(OnCloseGateEvent);
        }
    }

    public override void OnStopServer()
    {
        // Unsubscribe to avoid callbacks after this object is destroyed
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe<OpenGateEvent>(OnOpenGateEvent);
            EventManager.Instance.Unsubscribe<CloseGateEvent>(OnCloseGateEvent);
        }
        base.OnStopServer();
    }

    private void Update()
    {
        if (!isServer) return;

        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            OpenGate();
        }
        else if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
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
        // Guard against destroyed or missing references
        if (gateCollider != null)
        {
            gateCollider.enabled = !isOpen;
        }
        else
        {
            // Optional: log once to help diagnose setup issues
            Debug.LogWarning($"[{nameof(GateController)}] gateCollider is missing for gateId {gateId}.");
        }

        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = !isOpen;
        }

        if (isOpen)
        {
            EventManager.Instance.Publish(new OpenedGateEvent(gateId));
        }
        else
        {
            EventManager.Instance.Publish(new ClosedGateEvent(gateId));
        }

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        if (gateObject != null && isActiveAndEnabled)
        {
            moveCoroutine = StartCoroutine(MoveGate(targetPosition));
        }
    }

    private void OnIsOpenChanged(bool oldValue, bool newValue)
    {
        if (isServer) return;
        ChangeGateState();
    }

    private System.Collections.IEnumerator MoveGate(Vector3 targetPosition)
    {
        if (gateObject == null)
        {
            yield break;
        }

        Vector3 startPosition = gateObject.transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            if (gateObject == null)
            {
                yield break;
            }
            gateObject.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        gateObject.transform.position = targetPosition;
        moveCoroutine = null;
    }

    public void OnOpenGateEvent(OpenGateEvent openGateEvent)
    {
        if (openGateEvent.GateId == gateId)
        {
            OpenGate();
        }
    }
    public void OnCloseGateEvent(CloseGateEvent closeGateEvent)
    {
        if (closeGateEvent.GateId == gateId)
        {
            CloseGate();
        }
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
