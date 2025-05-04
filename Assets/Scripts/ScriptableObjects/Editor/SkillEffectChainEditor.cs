using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillEffectChainData))]
public class SkillEffectChainEditor : Editor
{
    private SerializedProperty rootNodesProp;

    private void OnEnable()
    {
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Find property if not already found
        rootNodesProp ??= serializedObject.FindProperty("rootNodes");

        EditorGUILayout.LabelField("Skill Effect Chain Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Sanitize Button
        if (GUILayout.Button("Sanitize Root Nodes (Remove Nulls)"))
        {
            for (int i = rootNodesProp.arraySize - 1; i >= 0; i--)
            {
                var element = rootNodesProp.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue == null)
                {
                    rootNodesProp.DeleteArrayElementAtIndex(i);
                }
            }

            // Apply changes after modifying the list
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update(); // Refresh
        }

        if (rootNodesProp != null)
        {
            for (int i = 0; i < rootNodesProp.arraySize; i++)
            {
                var rootNode = rootNodesProp.GetArrayElementAtIndex(i);

                if (rootNode.managedReferenceValue == null)
                {
                    EditorGUILayout.HelpBox($"Root Node {i} is null.", MessageType.Warning);
                    if (GUILayout.Button($"Initialize Root Node {i}"))
                    {
                        rootNode.managedReferenceValue = new SkillEffectNodeData();
                    }
                    continue;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Root Node {i}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    rootNodesProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                DrawNode(rootNode, 1);
                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Add Root Node"))
            {
                rootNodesProp.arraySize++;
                var newNode = rootNodesProp.GetArrayElementAtIndex(rootNodesProp.arraySize - 1);
                newNode.managedReferenceValue = new SkillEffectNodeData();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
    private void DrawNode(SerializedProperty nodeProp, int indent)
    {
        if (nodeProp == null) return;

        var effectProp = nodeProp.FindPropertyRelative("effect");
        var childrenProp = nodeProp.FindPropertyRelative("children");

        EditorGUILayout.BeginVertical("box");

        // Titel
        EditorGUILayout.LabelField($"Node (Depth {indent})", EditorStyles.boldLabel);

        // Effektfeld
        EditorGUILayout.PropertyField(effectProp, new GUIContent("Effect"));

        // Effekt-Subinspektor (wenn ScriptableObject verlinkt ist)
        var effectObject = effectProp.objectReferenceValue;
        if (effectObject != null)
        {
            Editor editor = CreateEditor(effectObject);
            if (editor != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Effect Properties", EditorStyles.boldLabel);
                editor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.Space();

        // Child hinzufügen Button
        if (GUILayout.Button("Add Child Node"))
        {
            childrenProp.arraySize++;
            childrenProp.GetArrayElementAtIndex(childrenProp.arraySize - 1).managedReferenceValue = new SkillEffectNodeData();
        }

        // Child-Nodes anzeigen
        for (int i = 0; i < childrenProp.arraySize; i++)
        {
            var child = childrenProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");

            // Titel und Remove-Button
            EditorGUILayout.BeginHorizontal();
            string arrows = new string('→', indent);
            EditorGUILayout.LabelField($"{arrows} Child {i}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                childrenProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            DrawNode(child, indent + 1); // rekursiver Aufruf

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }


}
