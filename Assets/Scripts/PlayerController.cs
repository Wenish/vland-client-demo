using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Game.Scripts.Controllers;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public GameObject Unit;
    [SyncVar]
    public float horizontalInput = 0f;
    [SyncVar]
    public float verticalInput = 0f;
    [SyncVar]
    public float angle = 0f;
    [SyncVar]
    public bool isPressingFire1 = false;

    private UnitController _unitController;

    private ControllerCamera _controllerCamera;
    // Start is called before the first frame update
    void Start()
    {
        if (isServer) {
            SpawnPlayerUnit();
        }

        if (isLocalPlayer) {
            _controllerCamera = Camera.main.GetComponent<ControllerCamera>();
            SetCameraTargetToPlayerUnit();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            InputAxis();
        }

        if(isServer) {
            ControlUnit();
        }
    }

    [Server]
    void SpawnPlayerUnit() {
        var unit = NetworkManager.Instantiate(CustomNetworkManager.singleton.spawnPrefabs[0]);
        NetworkServer.Spawn(unit);
        Unit = unit;
        _unitController = Unit.GetComponent<UnitController>();
    }

    [Client]
    void SetCameraTargetToPlayerUnit() {
        _controllerCamera.CameraTarget = Unit.transform;
    }

    [Client]
    void InputAxis()
    {
        var newHorizontalInput = Input.GetAxisRaw("Horizontal");
        var hasHorizontalInputChanged = newHorizontalInput != horizontalInput;
        var newVerticalInput = Input.GetAxisRaw("Vertical");
        var hasVerticalInputChanged = newVerticalInput != verticalInput;
        if (hasHorizontalInputChanged || hasVerticalInputChanged)
        {
            CmdSetInput(newHorizontalInput, newVerticalInput);
        }
    }

    [Command]
    void CmdSetInput(float horizontal, float vertical)
    {
        horizontalInput = horizontal;
        verticalInput = vertical;
    }

    [Server]
    void ControlUnit()
    {
        if (!_unitController) return;
        _unitController.horizontalInput = horizontalInput;
        _unitController.verticalInput = verticalInput;
        _unitController.angle = angle;

        
        if (isPressingFire1)
        {
            _unitController.Attack();
        }
    }
}
