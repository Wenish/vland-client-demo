using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TeamBarrier))]
public class TeamBarrierEditor : Editor
{
    private const float TriggerPadding = 0.4f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        string validationMessage = BuildValidationMessage((TeamBarrier)target);
        if (!string.IsNullOrEmpty(validationMessage))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto Setup Barrier Colliders"))
        {
            AutoSetup((TeamBarrier)target);
        }
    }

    private static string BuildValidationMessage(TeamBarrier teamBarrier)
    {
        if (teamBarrier == null)
        {
            return string.Empty;
        }

        SerializedObject serializedBarrier = new SerializedObject(teamBarrier);
        SerializedProperty barrierColliderProperty = serializedBarrier.FindProperty("barrierCollider");
        SerializedProperty detectionTriggerProperty = serializedBarrier.FindProperty("detectionTrigger");

        Collider barrierCollider = barrierColliderProperty.objectReferenceValue as Collider;
        Collider detectionTrigger = detectionTriggerProperty.objectReferenceValue as Collider;

        System.Collections.Generic.List<string> issues = new System.Collections.Generic.List<string>();

        if (barrierCollider == null)
        {
            issues.Add("Barrier Collider is missing.");
        }
        else
        {
            if (barrierCollider.gameObject != teamBarrier.gameObject)
            {
                issues.Add("Barrier Collider must be on the same GameObject as TeamBarrier.");
            }

            if (barrierCollider.isTrigger)
            {
                issues.Add("Barrier Collider must not be a trigger.");
            }
        }

        if (detectionTrigger == null)
        {
            issues.Add("Detection Trigger is missing.");
        }
        else
        {
            if (detectionTrigger.gameObject != teamBarrier.gameObject)
            {
                issues.Add("Detection Trigger must be on the same GameObject as TeamBarrier.");
            }

            if (!detectionTrigger.isTrigger)
            {
                issues.Add("Detection Trigger must be set to Is Trigger.");
            }
        }

        if (barrierCollider != null && detectionTrigger != null && barrierCollider == detectionTrigger)
        {
            issues.Add("Barrier Collider and Detection Trigger cannot be the same collider.");
        }

        if (issues.Count == 0)
        {
            return string.Empty;
        }

        return "TeamBarrier setup issues:\n- " + string.Join("\n- ", issues);
    }

    private static void AutoSetup(TeamBarrier teamBarrier)
    {
        if (teamBarrier == null)
        {
            return;
        }

        SerializedObject serializedBarrier = new SerializedObject(teamBarrier);
        SerializedProperty barrierColliderProperty = serializedBarrier.FindProperty("barrierCollider");
        SerializedProperty detectionTriggerProperty = serializedBarrier.FindProperty("detectionTrigger");

        Collider barrierCollider = barrierColliderProperty.objectReferenceValue as Collider;
        Collider detectionTrigger = detectionTriggerProperty.objectReferenceValue as Collider;

        Collider[] colliders = teamBarrier.GetComponents<Collider>();

        if (!IsValidColliderReference(teamBarrier, barrierCollider))
        {
            barrierCollider = FindFirstNonTrigger(colliders);
        }

        if (barrierCollider == null && colliders.Length > 0)
        {
            barrierCollider = colliders[0];
        }

        if (barrierCollider == null)
        {
            Debug.LogWarning($"[{nameof(TeamBarrierEditor)}] No collider found on '{teamBarrier.name}'. Add a collider first.", teamBarrier);
            return;
        }

        Undo.RecordObject(barrierCollider, "Configure TeamBarrier Barrier Collider");
        barrierCollider.isTrigger = false;

        if (!IsValidColliderReference(teamBarrier, detectionTrigger) || detectionTrigger == barrierCollider)
        {
            detectionTrigger = FindFirstTrigger(colliders, barrierCollider);
        }

        if (detectionTrigger == null)
        {
            detectionTrigger = CreateTriggerCollider(teamBarrier.gameObject, barrierCollider);
            if (detectionTrigger == null)
            {
                Debug.LogWarning($"[{nameof(TeamBarrierEditor)}] Failed to create detection trigger for '{teamBarrier.name}'.", teamBarrier);
                return;
            }
        }

        Undo.RecordObject(detectionTrigger, "Configure TeamBarrier Detection Trigger");
        detectionTrigger.isTrigger = true;
        CopyColliderShape(barrierCollider, detectionTrigger, teamBarrier.transform, TriggerPadding);

        barrierColliderProperty.objectReferenceValue = barrierCollider;
        detectionTriggerProperty.objectReferenceValue = detectionTrigger;
        serializedBarrier.ApplyModifiedProperties();

        EditorUtility.SetDirty(barrierCollider);
        EditorUtility.SetDirty(detectionTrigger);
        EditorUtility.SetDirty(teamBarrier);

        Debug.Log($"[{nameof(TeamBarrierEditor)}] Auto setup complete on '{teamBarrier.name}'.", teamBarrier);
    }

    private static bool IsValidColliderReference(TeamBarrier teamBarrier, Collider collider)
    {
        return collider != null && collider.gameObject == teamBarrier.gameObject;
    }

    private static Collider FindFirstNonTrigger(Collider[] colliders)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].isTrigger)
            {
                return colliders[i];
            }
        }

        return null;
    }

    private static Collider FindFirstTrigger(Collider[] colliders, Collider excluded)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider current = colliders[i];
            if (current == excluded) continue;
            if (current.isTrigger)
            {
                return current;
            }
        }

        return null;
    }

    private static Collider CreateTriggerCollider(GameObject gameObject, Collider source)
    {
        if (source is BoxCollider)
        {
            return Undo.AddComponent<BoxCollider>(gameObject);
        }

        if (source is SphereCollider)
        {
            return Undo.AddComponent<SphereCollider>(gameObject);
        }

        if (source is CapsuleCollider)
        {
            return Undo.AddComponent<CapsuleCollider>(gameObject);
        }

        return Undo.AddComponent<BoxCollider>(gameObject);
    }

    private static void CopyColliderShape(Collider source, Collider target, Transform root, float padding)
    {
        if (source is BoxCollider sourceBox && target is BoxCollider targetBox)
        {
            targetBox.center = sourceBox.center;
            targetBox.size = sourceBox.size + Vector3.one * (padding * 2f);
            return;
        }

        if (source is SphereCollider sourceSphere && target is SphereCollider targetSphere)
        {
            targetSphere.center = sourceSphere.center;
            targetSphere.radius = sourceSphere.radius + padding;
            return;
        }

        if (source is CapsuleCollider sourceCapsule && target is CapsuleCollider targetCapsule)
        {
            targetCapsule.center = sourceCapsule.center;
            targetCapsule.direction = sourceCapsule.direction;
            targetCapsule.radius = sourceCapsule.radius + padding;
            targetCapsule.height = sourceCapsule.height + (padding * 2f);
            return;
        }

        Bounds worldBounds = source.bounds;
        Vector3 paddedWorldSize = worldBounds.size + Vector3.one * (padding * 2f);
        Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = DivideByLossyScale(paddedWorldSize, root.lossyScale);

        if (target is BoxCollider targetFallbackBox)
        {
            targetFallbackBox.center = localCenter;
            targetFallbackBox.size = localSize;
            return;
        }

        if (target is SphereCollider targetFallbackSphere)
        {
            targetFallbackSphere.center = localCenter;
            targetFallbackSphere.radius = Mathf.Max(localSize.x, localSize.y, localSize.z) * 0.5f;
            return;
        }

        if (target is CapsuleCollider targetFallbackCapsule)
        {
            targetFallbackCapsule.center = localCenter;
            targetFallbackCapsule.direction = 1;
            targetFallbackCapsule.height = Mathf.Max(localSize.y, Mathf.Max(localSize.x, localSize.z));
            targetFallbackCapsule.radius = Mathf.Max(localSize.x, localSize.z) * 0.5f;
        }
    }

    private static Vector3 DivideByLossyScale(Vector3 worldSize, Vector3 lossyScale)
    {
        return new Vector3(
            SafeDivide(worldSize.x, lossyScale.x),
            SafeDivide(worldSize.y, lossyScale.y),
            SafeDivide(worldSize.z, lossyScale.z)
        );
    }

    private static float SafeDivide(float value, float scale)
    {
        float safeScale = Mathf.Abs(scale);
        if (safeScale < 0.0001f) safeScale = 0.0001f;
        return value / safeScale;
    }
}