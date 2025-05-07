using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillEffectChainData))]
public class SkillEffectChainEditor : Editor
{
    private SerializedProperty rootNodesProp;

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

                // Draw root node without indent
                DrawNode(rootNode, 0);
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

        // Determine background color based on effect type
        Color originalBg = GUI.backgroundColor;
        Object effectObject = effectProp.objectReferenceValue;
        string effectType = effectObject != null ? GetEffectType(effectObject) : null;
        if (effectType != null)
        {
            switch (effectType)
            {
                case nameof(SkillEffectType.Mechanic):
                    GUI.backgroundColor = new Color(0.8f, 0f, 0.8f); // Purple
                    break;
                case nameof(SkillEffectType.Condition):
                    GUI.backgroundColor = new Color(0f, 0f, 1f); // Blue
                    break;
                case nameof(SkillEffectType.Target):
                    GUI.backgroundColor = new Color(0f, 1f, 0f); // Green
                    break;
            }
        }

        // Begin indent horizontal group
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indent * 15); // indent per level
        EditorGUILayout.BeginVertical("box");
        GUILayout.Space(4);
        // Reset background for content
        GUI.backgroundColor = originalBg;

        // Header with effect type
        EditorGUILayout.BeginHorizontal();
        string arrows = indent > 0 ? new string('→', indent) + " " : string.Empty;
        string displayEffectType = effectType != null ? $" - {effectType}" : string.Empty;
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };
        EditorGUILayout.LabelField($"{arrows}Node (Depth {indent}){displayEffectType}", headerStyle);
        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            // Remove this node from parent array
            string path = nodeProp.propertyPath;
            if (path.Contains("Array.data"))
            {
                string parentPath = path.Substring(0, path.LastIndexOf('.'));
                var parentArray = nodeProp.serializedObject.FindProperty(parentPath);
                int index = int.Parse(path.Substring(path.LastIndexOf('[') + 1, path.LastIndexOf(']') - path.LastIndexOf('[') - 1));
                parentArray.DeleteArrayElementAtIndex(index);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = originalBg;
            return;
        }
        EditorGUILayout.EndHorizontal();

        // Effect field
        EditorGUILayout.PropertyField(effectProp, new GUIContent("Effect"));

        // Effect sub-inspector
        if (effectObject != null)
        {
            InspectorTitlebar(true, effectObject);
        }

        EditorGUILayout.Space();

        // Children button
        if (GUILayout.Button("Add Child Node"))
        {
            childrenProp.arraySize++;
            childrenProp.GetArrayElementAtIndex(childrenProp.arraySize - 1).managedReferenceValue = new SkillEffectNodeData();
        }

        // Draw children
        for (int i = 0; i < childrenProp.arraySize; i++)
        {
            var child = childrenProp.GetArrayElementAtIndex(i);
            DrawNode(child, indent + 1);
            GUILayout.Space(5);
        }

        // End groups
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = originalBg;
    }

    private string GetEffectType(Object effectObject)
    {
        var effectTypeProperty = effectObject.GetType().GetProperty("EffectType");
        if (effectTypeProperty != null)
        {
            var value = effectTypeProperty.GetValue(effectObject);
            return value != null ? value.ToString() : null;
        }
        return null;
    }

    private void InspectorTitlebar(bool expanded, Object target)
    {
        Editor editor = CreateEditor(target);
        if (editor != null)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Effect Properties", EditorStyles.boldLabel);
            editor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }
    }
}
