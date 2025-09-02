using Mirror;
using UnityEngine;

public class PlayerInput : NetworkBehaviour
{

    [ClientCallback]
    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdDoSomething("Hello from client: " + gameObject.name);
        }
    }

    [Command]
    public void CmdDoSomething(string message)
    {
        Debug.Log("Command executed on server by " + connectionToClient.connectionId + ": " + message);
    }

    [ClientRpc]
    public void RpcDoSomething()
    {
        // This code runs on all clients
    }
}