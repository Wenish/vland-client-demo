using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(SkirmishGameManager))]
public class SkirmishGatesManager : MonoBehaviour
{
    SkirmishGameManager gameManager;
    List<GateController> gateControllers = new List<GateController>();
    

    private void Awake()
    {
        gameManager = FindAnyObjectByType<SkirmishGameManager>();

        if (gameManager == null)
        {
            Debug.LogError("SkirmishGameManager not found in scene.");
        }

        gateControllers = new List<GateController>(FindObjectsByType<GateController>());
        
        Debug.Log($"[SkirmishGatesManager] Found {gateControllers.Count} gates in the scene.");
    }

    private void OnEnable()
    {
        if (!NetworkServer.active)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnRoundStateChanged += OnRoundStateChanged;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnRoundStateChanged -= OnRoundStateChanged;
        }
    }

    private void OnRoundStateChanged(SkirmishGameManager.RoundState newState)
    {
        if (!NetworkServer.active)
        {
            return;
        }
        
        gateControllers = new List<GateController>(FindObjectsByType<GateController>());
        Debug.Log($"[SkirmishGatesManager] Round state changed to {newState}. Found {gateControllers.Count} gates in the scene.");

        switch (newState)
        {
            case SkirmishGameManager.RoundState.WaitingToStart:
                CloseAllGates();
                break;
            case SkirmishGameManager.RoundState.PreRoundCountdown:
                CloseAllGates();
                break;
            case SkirmishGameManager.RoundState.InRound:
                OpenAllGates();
                break;
        }
    }

    private void CloseAllGates()
    {
        foreach (var gate in gateControllers)
        {
            gate.CloseGate();
        }
    }

    private void OpenAllGates()
    {
        foreach (var gate in gateControllers)
        {
            gate.OpenGate();
        }
    }
}
