using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DestructibleObjective))]
public class DestructibleObjectiveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var objective = (DestructibleObjective)target;
        var issues = BuildValidationIssues(objective);

        if (issues.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Destructible setup issues:\n- " + string.Join("\n- ", issues), MessageType.Warning);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Auto Setup Basic Destructible"))
        {
            AutoSetupBasicDestructible(objective);
        }

        if (IsEditingPrefabAsset(objective.gameObject) && GUILayout.Button("Add Prefab To NetworkManager Spawnables"))
        {
            TryAddPrefabToNetworkManagerSpawnables(objective.gameObject);
        }
    }

    private static List<string> BuildValidationIssues(DestructibleObjective objective)
    {
        var issues = new List<string>();
        if (objective == null)
        {
            issues.Add("Objective reference is null.");
            return issues;
        }

        var go = objective.gameObject;

        if (go.GetComponent<NetworkIdentity>() == null)
        {
            issues.Add("Missing NetworkIdentity.");
        }

        var unitController = go.GetComponent<UnitController>();
        if (unitController == null)
        {
            issues.Add("Missing UnitController.");
        }
        else
        {
            if (unitController.unitType != UnitType.Structure)
            {
                issues.Add("UnitController unitType is not Structure (recommended for destructibles).");
            }

            if (unitController.maxHealth <= 0)
            {
                issues.Add("UnitController maxHealth should be > 0.");
            }

            if (unitController.moveSpeed != 0f)
            {
                issues.Add("UnitController moveSpeed is not 0 (recommended for static destructibles).");
            }
        }

        if (go.GetComponent<UnitMediator>() == null)
        {
            issues.Add("Missing UnitMediator (required by UnitController movement/stat reads).");
        }

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            issues.Add("Missing Rigidbody.");
        }
        else
        {
            if (!rb.isKinematic)
            {
                issues.Add("Rigidbody is not kinematic (recommended for static destructibles).");
            }

            if (rb.useGravity)
            {
                issues.Add("Rigidbody gravity is enabled (recommended off for static destructibles).");
            }
        }

        var ownCollider = go.GetComponent<Collider>();
        if (ownCollider == null)
        {
            issues.Add("No Collider on same GameObject. Hits are resolved on the hit collider and should find UnitController directly.");
        }
        else if (ownCollider.isTrigger)
        {
            issues.Add("Primary collider is trigger; recommended non-trigger for being hittable by projectiles/melee.");
        }

        if (IsEditingPrefabAsset(go))
        {
            if (!IsPrefabInNetworkManagerSpawnables(go))
            {
                issues.Add("Prefab is not listed in NetworkManager spawnPrefabs (required only if spawning at runtime).");
            }
        }

        return issues;
    }

    private static void AutoSetupBasicDestructible(DestructibleObjective objective)
    {
        if (objective == null)
        {
            return;
        }

        var go = objective.gameObject;

        var identity = go.GetComponent<NetworkIdentity>();
        if (identity == null)
        {
            identity = Undo.AddComponent<NetworkIdentity>(go);
        }

        var unitController = go.GetComponent<UnitController>();
        if (unitController == null)
        {
            unitController = Undo.AddComponent<UnitController>(go);
        }

        var mediator = go.GetComponent<UnitMediator>();
        if (mediator == null)
        {
            mediator = Undo.AddComponent<UnitMediator>(go);
        }

        var rigidbody = go.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = Undo.AddComponent<Rigidbody>(go);
        }

        var collider = go.GetComponent<Collider>();
        if (collider == null)
        {
            collider = Undo.AddComponent<BoxCollider>(go);
        }

        Undo.RecordObject(unitController, "Configure UnitController For Destructible");
        if (unitController.maxHealth <= 0)
        {
            unitController.maxHealth = 200;
        }
        unitController.unitType = UnitType.Structure;
        unitController.health = Mathf.Clamp(unitController.health <= 0 ? unitController.maxHealth : unitController.health, 1, unitController.maxHealth);
        unitController.maxShield = 0;
        unitController.shield = 0;
        unitController.moveSpeed = 0f;

        Undo.RecordObject(rigidbody, "Configure Rigidbody For Destructible");
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;

        Undo.RecordObject(collider, "Configure Collider For Destructible");
        collider.isTrigger = false;

        EditorUtility.SetDirty(identity);
        EditorUtility.SetDirty(unitController);
        EditorUtility.SetDirty(mediator);
        EditorUtility.SetDirty(rigidbody);
        EditorUtility.SetDirty(collider);
        EditorUtility.SetDirty(objective);

        Debug.Log($"[{nameof(DestructibleObjectiveEditor)}] Auto setup complete for '{go.name}'.", go);
    }

    private static bool IsEditingPrefabAsset(GameObject go)
    {
        return go != null && PrefabUtility.IsPartOfPrefabAsset(go);
    }

    private static bool IsPrefabInNetworkManagerSpawnables(GameObject prefabAsset)
    {
        var networkManager = Object.FindAnyObjectByType<NetworkManager>();
        if (networkManager == null || networkManager.spawnPrefabs == null)
        {
            return false;
        }

        for (int i = 0; i < networkManager.spawnPrefabs.Count; i++)
        {
            if (networkManager.spawnPrefabs[i] == prefabAsset)
            {
                return true;
            }
        }

        return false;
    }

    private static void TryAddPrefabToNetworkManagerSpawnables(GameObject prefabAsset)
    {
        if (prefabAsset == null)
        {
            return;
        }

        var networkManager = Object.FindAnyObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogWarning($"[{nameof(DestructibleObjectiveEditor)}] No NetworkManager found in open scene. Open the scene with NetworkManager first.");
            return;
        }

        if (networkManager.spawnPrefabs == null)
        {
            networkManager.spawnPrefabs = new List<GameObject>();
        }

        if (!networkManager.spawnPrefabs.Contains(prefabAsset))
        {
            Undo.RecordObject(networkManager, "Add Spawnable Destructible Prefab");
            networkManager.spawnPrefabs.Add(prefabAsset);
            EditorUtility.SetDirty(networkManager);
            Debug.Log($"[{nameof(DestructibleObjectiveEditor)}] Added '{prefabAsset.name}' to NetworkManager spawnPrefabs.", networkManager);
        }
        else
        {
            Debug.Log($"[{nameof(DestructibleObjectiveEditor)}] '{prefabAsset.name}' is already in NetworkManager spawnPrefabs.", networkManager);
        }
    }
}