using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(CastleSiegeManager))]
public class CastleSiegeGateManager : MonoBehaviour
{
    CastleSiegeManager gameManager;
    List<GateController> gateControllers = new List<GateController>();
    

    private void Awake()
    {
        gameManager = FindAnyObjectByType<CastleSiegeManager>();

        if (gameManager == null)
        {
            Debug.LogError("CastleSiegeManager not found in scene.");
        }

        gateControllers = new List<GateController>(FindObjectsByType<GateController>());
        
        Debug.Log($"[CastleSiegeGateManager] Found {gateControllers.Count} gates in the scene.");
    }

    private void OnEnable()
    {
        if (!NetworkServer.active)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnMatchPhaseChanged += OnMatchPhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnMatchPhaseChanged -= OnMatchPhaseChanged;
        }
    }

    private void OnMatchPhaseChanged(CastleSiegeManager.MatchPhase newPhase)
    {
        if (!NetworkServer.active)
        {
            return;
        }
        
        gateControllers = new List<GateController>(FindObjectsByType<GateController>());
        Debug.Log($"[CastleSiegeGateManager] Match phase changed to {newPhase}. Found {gateControllers.Count} gates in the scene.");

        switch (newPhase)
        {
            case CastleSiegeManager.MatchPhase.Warmup:
                CloseAllGates();
                break;
            case CastleSiegeManager.MatchPhase.InGame:
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
