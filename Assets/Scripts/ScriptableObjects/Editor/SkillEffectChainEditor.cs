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

        EditorGUI.indentLevel = indent;

        var effectProp = nodeProp.FindPropertyRelative("effect");

        var childrenProp = nodeProp.FindPropertyRelative("children");

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(new GUIContent($"Node (Depth {indent})"), EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effectProp, new GUIContent("Effect"));

        // If the effect is a ScriptableObject reference, draw its fields
        var effectObject = effectProp.objectReferenceValue;
        if (effectObject != null)
        {
            EditorGUILayout.Space(2);
            Editor editor = CreateEditor(effectObject);
            if (editor != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Effect Properties", EditorStyles.boldLabel);
                editor.OnInspectorGUI(); // draws fields like healAmount
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indent * 8); // visual offset
        if (GUILayout.Button("Add Child Node"))
        {
            childrenProp.arraySize++;
            childrenProp.GetArrayElementAtIndex(childrenProp.arraySize - 1).managedReferenceValue = new SkillEffectNodeData();
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < childrenProp.arraySize; i++)
        {
            var child = childrenProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 8); // offset per hierarchy level
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"→ Child {i}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                childrenProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();

            DrawNode(child, indent + 1); // recursion

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

}
