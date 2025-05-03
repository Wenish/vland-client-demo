

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTrigger", menuName = "Game/Triggers/Trigger")]
public class SkillEffectTriggerData : SkillEffectData
{
    public bool isRunning { get; private set; } = false;
    public override bool Execute(UnitController unit, List<UnitController> targets)
    {
        try {
            if (isRunning)
            {
                Debug.LogWarning("SkillEffectTriggerData is already running.");
                return false;
            }

            isRunning = true;

            // Execute the effect
            bool isExecutionSuccessful = ExecuteChildren(unit, targets);

            // Handle success or failure
            if (isExecutionSuccessful)
            {
                Debug.Log("SkillEffectTriggerData executed successfully.");
            }
            else
            {
                Debug.LogWarning("SkillEffectTriggerData execution failed.");
            }

            return isExecutionSuccessful;
        }
        finally
        {
            isRunning = false;
        }
    }
}