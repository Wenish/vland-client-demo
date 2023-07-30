using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Game.Scripts.Controllers;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public GameObject Unit;

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
        
    }

    [Server]
    void SpawnPlayerUnit() {
        var unit = NetworkManager.Instantiate(CustomNetworkManager.singleton.spawnPrefabs[0]);
        NetworkServer.Spawn(unit);
        Unit = unit;
    }

    [Client]
    void SetCameraTargetToPlayerUnit() {
        _controllerCamera.CameraTarget = Unit.transform;
    }
}
