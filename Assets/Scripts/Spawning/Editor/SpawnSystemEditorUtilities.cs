#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor utilities for the spawn system.
/// Provides context menu helpers and editor-only functionality.
/// </summary>
public static class SpawnSystemEditorUtilities
{
    // ===== CONTEXT MENU HELPERS =====

    /// <summary>
    /// Create a mob spawner at the current scene view camera position.
    /// Right-click in Hierarchy → Spawn System → Create Mob Spawner
    /// </summary>
    [MenuItem("GameObject/Spawn System/Create Mob Spawner", false, 10)]
    public static void CreateMobSpawner()
    {
        GameObject spawner = new GameObject("MobSpawner");
        spawner.AddComponent<MobSpawner>();
        
        // Position at scene view camera
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 position = SceneView.lastActiveSceneView.camera.transform.position;
            position += SceneView.lastActiveSceneView.camera.transform.forward * 5f;
            spawner.transform.position = position;
        }

        Selection.activeGameObject = spawner;
        Undo.RegisterCreatedObjectUndo(spawner, "Create Mob Spawner");
        
        Debug.Log("Created Mob Spawner. Assign a spawn configuration in the Inspector.");
    }

    /// <summary>
    /// Create a boss spawner at the current scene view camera position.
    /// Right-click in Hierarchy → Spawn System → Create Boss Spawner
    /// </summary>
    [MenuItem("GameObject/Spawn System/Create Boss Spawner", false, 11)]
    public static void CreateBossSpawner()
    {
        GameObject spawner = new GameObject("BossSpawner");
        spawner.AddComponent<BossSpawner>();
        
        // Position at scene view camera
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 position = SceneView.lastActiveSceneView.camera.transform.position;
            position += SceneView.lastActiveSceneView.camera.transform.forward * 5f;
            spawner.transform.position = position;
        }

        Selection.activeGameObject = spawner;
        Undo.RegisterCreatedObjectUndo(spawner, "Create Boss Spawner");
        
        Debug.Log("Created Boss Spawner. Assign a spawn configuration in the Inspector.");
    }

    /// <summary>
    /// Create a boss trigger at the current scene view camera position.
    /// Right-click in Hierarchy → Spawn System → Create Boss Trigger
    /// </summary>
    [MenuItem("GameObject/Spawn System/Create Boss Trigger", false, 12)]
    public static void CreateBossTrigger()
    {
        GameObject trigger = new GameObject("BossTrigger");
        BoxCollider collider = trigger.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(10f, 5f, 10f);
        trigger.AddComponent<BossEncounterTrigger>();
        
        // Position at scene view camera
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 position = SceneView.lastActiveSceneView.camera.transform.position;
            position += SceneView.lastActiveSceneView.camera.transform.forward * 5f;
            trigger.transform.position = position;
        }

        Selection.activeGameObject = trigger;
        Undo.RegisterCreatedObjectUndo(trigger, "Create Boss Trigger");
        
        Debug.Log("Created Boss Trigger. Assign a boss spawner in the Inspector and adjust the collider size.");
    }

    /// <summary>
    /// Create a spawn manager in the scene.
    /// Right-click in Hierarchy → Spawn System → Create Spawn Manager
    /// </summary>
    [MenuItem("GameObject/Spawn System/Create Spawn Manager", false, 13)]
    public static void CreateSpawnManager()
    {
        // Check if one already exists
        SpawnManager existing = GameObject.FindFirstObjectByType<SpawnManager>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog(
                "Spawn Manager Exists",
                "A Spawn Manager already exists in the scene. Only one is needed per scene.",
                "OK"
            );
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject manager = new GameObject("SpawnManager");
        manager.AddComponent<SpawnManager>();

        Selection.activeGameObject = manager;
        Undo.RegisterCreatedObjectUndo(manager, "Create Spawn Manager");
        
        Debug.Log("Created Spawn Manager. It will auto-discover all spawners on server start.");
    }

    // ===== VALIDATION HELPERS =====

    /// <summary>
    /// Validate all spawn configurations in the project.
    /// Tools → Spawn System → Validate All Configurations
    /// </summary>
    [MenuItem("Tools/Spawn System/Validate All Configurations")]
    public static void ValidateAllConfigurations()
    {
        int validCount = 0;
        int invalidCount = 0;
        
        // Find all mob spawn configurations
        string[] mobGuids = AssetDatabase.FindAssets("t:MobSpawnConfiguration");
        foreach (string guid in mobGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpawnConfigurationMob config = AssetDatabase.LoadAssetAtPath<SpawnConfigurationMob>(path);
            
            if (config != null)
            {
                if (config.Validate())
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogError($"Invalid configuration: {config.name} at {path}", config);
                }
            }
        }

        // Find all boss spawn configurations
        string[] bossGuids = AssetDatabase.FindAssets("t:BossSpawnConfiguration");
        foreach (string guid in bossGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpawnConfigurationBoss config = AssetDatabase.LoadAssetAtPath<SpawnConfigurationBoss>(path);
            
            if (config != null)
            {
                if (config.Validate())
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogError($"Invalid configuration: {config.name} at {path}", config);
                }
            }
        }

        string message = $"Validation Complete:\n{validCount} valid configurations\n{invalidCount} invalid configurations";
        
        if (invalidCount > 0)
        {
            EditorUtility.DisplayDialog("Validation Complete", message + "\n\nCheck Console for details.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Validation Complete", message + "\n\nAll configurations are valid!", "OK");
        }
        
        Debug.Log($"[Spawn System] {message}");
    }

    /// <summary>
    /// Find all spawners in the current scene.
    /// Tools → Spawn System → Find All Spawners
    /// </summary>
    [MenuItem("Tools/Spawn System/Find All Spawners")]
    public static void FindAllSpawners()
    {
        MobSpawner[] mobSpawners = GameObject.FindObjectsByType<MobSpawner>(FindObjectsSortMode.None);
        BossSpawner[] bossSpawners = GameObject.FindObjectsByType<BossSpawner>(FindObjectsSortMode.None);

        Debug.Log($"[Spawn System] Found {mobSpawners.Length} mob spawners and {bossSpawners.Length} boss spawners in scene");
        
        if (mobSpawners.Length == 0 && bossSpawners.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Spawners Found",
                "No spawners found in the current scene.",
                "OK"
            );
            return;
        }

        // Log details
        foreach (var spawner in mobSpawners)
        {
            string config = spawner.spawnConfiguration != null ? spawner.spawnConfiguration.name : "No Config";
            Debug.Log($"  Mob Spawner: {spawner.name} [{spawner.spawnerId}] - Config: {config}", spawner);
        }

        foreach (var spawner in bossSpawners)
        {
            string config = spawner.spawnConfiguration != null ? spawner.spawnConfiguration.name : "No Config";
            Debug.Log($"  Boss Spawner: {spawner.name} [{spawner.spawnerId}] - Config: {config}", spawner);
        }

        EditorUtility.DisplayDialog(
            "Spawners Found",
            $"Found:\n{mobSpawners.Length} mob spawners\n{bossSpawners.Length} boss spawners\n\nCheck Console for details.",
            "OK"
        );
    }

    // ===== UTILITY HELPERS =====

    /// <summary>
    /// Select all spawners in the scene.
    /// Tools → Spawn System → Select All Spawners
    /// </summary>
    [MenuItem("Tools/Spawn System/Select All Spawners")]
    public static void SelectAllSpawners()
    {
        MobSpawner[] mobSpawners = GameObject.FindObjectsByType<MobSpawner>(FindObjectsSortMode.None);
        BossSpawner[] bossSpawners = GameObject.FindObjectsByType<BossSpawner>(FindObjectsSortMode.None);

        GameObject[] allSpawners = new GameObject[mobSpawners.Length + bossSpawners.Length];
        
        for (int i = 0; i < mobSpawners.Length; i++)
            allSpawners[i] = mobSpawners[i].gameObject;
        
        for (int i = 0; i < bossSpawners.Length; i++)
            allSpawners[mobSpawners.Length + i] = bossSpawners[i].gameObject;

        if (allSpawners.Length == 0)
        {
            Debug.Log("[Spawn System] No spawners found in scene");
            return;
        }

        Selection.objects = allSpawners;
        Debug.Log($"[Spawn System] Selected {allSpawners.Length} spawners");
    }

    /// <summary>
    /// Generate unique spawner IDs for all spawners missing IDs.
    /// Tools → Spawn System → Generate Missing IDs
    /// </summary>
    [MenuItem("Tools/Spawn System/Generate Missing IDs")]
    public static void GenerateMissingIDs()
    {
        int generatedCount = 0;

        MobSpawner[] mobSpawners = GameObject.FindObjectsByType<MobSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in mobSpawners)
        {
            if (string.IsNullOrEmpty(spawner.spawnerId))
            {
                spawner.spawnerId = $"mob_spawner_{generatedCount:D3}";
                EditorUtility.SetDirty(spawner);
                generatedCount++;
                Debug.Log($"Generated ID for {spawner.name}: {spawner.spawnerId}", spawner);
            }
        }

        BossSpawner[] bossSpawners = GameObject.FindObjectsByType<BossSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in bossSpawners)
        {
            if (string.IsNullOrEmpty(spawner.spawnerId))
            {
                spawner.spawnerId = $"boss_spawner_{generatedCount:D3}";
                EditorUtility.SetDirty(spawner);
                generatedCount++;
                Debug.Log($"Generated ID for {spawner.name}: {spawner.spawnerId}", spawner);
            }
        }

        if (generatedCount > 0)
        {
            EditorUtility.DisplayDialog(
                "IDs Generated",
                $"Generated {generatedCount} spawner IDs.\nCheck Console for details.",
                "OK"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "No Missing IDs",
                "All spawners already have IDs assigned.",
                "OK"
            );
        }
    }
}
#endif
