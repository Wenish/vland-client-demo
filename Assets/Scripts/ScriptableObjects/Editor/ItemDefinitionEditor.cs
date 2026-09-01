using ShadowInfection.Items;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDefinition))]
public sealed class ItemDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawIdentity();
        DrawKind();

        var kind = (ItemKind)serializedObject.FindProperty("kind").enumValueIndex;
        var rarity = (ItemRarity)serializedObject.FindProperty("rarity").enumValueIndex;
        var slot = (ItemSlot)serializedObject.FindProperty("slot").enumValueIndex;

        if (kind == ItemKind.Equipment)
            DrawEquipment(slot, rarity);
        else if (kind == ItemKind.Gem)
            DrawGem();
        else if (kind == ItemKind.Material)
            DrawMaterial();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("activeSkill"));
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentity()
    {
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
        EditorGUILayout.Space();
    }

    private void DrawKind()
    {
        EditorGUILayout.LabelField("Kind", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("kind"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"));
        EditorGUILayout.Space();
    }

    private void DrawEquipment(ItemSlot slot, ItemRarity rarity)
    {
        EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("slot"));

        if (ItemRules.IsArmorSlot(slot))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armorWeight"));

        if (slot == ItemSlot.MainHand || slot == ItemSlot.OffHand)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponData"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("statModifiers"), true);
        if (rarity == ItemRarity.Legendary)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("legendaryBuff"));
        EditorGUILayout.Space();
    }

    private void DrawGem()
    {
        EditorGUILayout.LabelField("Gem", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("statModifiers"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("keyword"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("keywordBonus"), true);
        EditorGUILayout.Space();
    }

    private void DrawMaterial()
    {
        EditorGUILayout.HelpBox("Stackable crafting ingredient. No extra fields.", MessageType.Info);
        EditorGUILayout.Space();
    }
}
