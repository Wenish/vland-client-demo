using UnityEditor;
using UnityEngine;

public static class SkillIndicatorDefaultsMenu
{
    private const string Folder = "Assets/Resources/ScriptableObjects/SkillIndicators";

    [MenuItem("Game/Skills/Create Default Skill Indicators")]
    public static void CreateDefaults()
    {
        EnsureFolder(Folder);

        var selfCircle = CreateIndicator(
            "DefaultSelfCircleIndicator",
            SkillIndicatorData.IndicatorShape.Circle,
            SkillIndicatorData.IndicatorPlacement.Self,
            showRangeRing: false,
            overrideRadius: 3f);

        var groundCircle = CreateIndicator(
            "DefaultGroundCircleIndicator",
            SkillIndicatorData.IndicatorShape.Circle,
            SkillIndicatorData.IndicatorPlacement.AtAimPoint,
            showRangeRing: true,
            overrideRadius: 2.5f);

        var directional = CreateIndicator(
            "DefaultDirectionalIndicator",
            SkillIndicatorData.IndicatorShape.Directional,
            SkillIndicatorData.IndicatorPlacement.FromCasterTowardAim,
            showRangeRing: true,
            overrideRange: 8f,
            overrideWidth: 1.5f);

        var cone = CreateIndicator(
            "DefaultConeIndicator",
            SkillIndicatorData.IndicatorShape.Cone,
            SkillIndicatorData.IndicatorPlacement.FromCasterTowardAim,
            showRangeRing: false,
            overrideRange: 5f,
            overrideAngle: 90f);

        Selection.objects = new Object[] { selfCircle, groundCircle, directional, cone };
        Debug.Log(
            "[SkillIndicators] Created default indicators in " + Folder
            + ". Assign aimPreviewIndicator on SkillData and add Show Indicator mechanics to cast chains.");
    }

    private static SkillIndicatorData CreateIndicator(
        string assetName,
        SkillIndicatorData.IndicatorShape shape,
        SkillIndicatorData.IndicatorPlacement placement,
        bool showRangeRing,
        float overrideRadius = 0f,
        float overrideRange = 0f,
        float overrideWidth = 0f,
        float overrideAngle = 0f)
    {
        string path = $"{Folder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SkillIndicatorData>(path);
        if (existing != null)
            return existing;

        var asset = ScriptableObject.CreateInstance<SkillIndicatorData>();
        asset.shape = shape;
        asset.placement = placement;
        asset.aimFollowMode = SkillIndicatorData.AimFollowMode.FollowWhileActive;
        asset.showRangeRing = showRangeRing;
        asset.overrideRadius = overrideRadius;
        asset.overrideRange = overrideRange;
        asset.overrideWidth = overrideWidth;
        asset.overrideAngle = overrideAngle;

        var rangeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Resources/SkillIndicators/rangeskillindicator.png");
        string placementPath = shape == SkillIndicatorData.IndicatorShape.Cone
            ? "Assets/Resources/SkillIndicators/coneskillindicator.png"
            : "Assets/Resources/SkillIndicators/aoeskillindicator_nobackground.png";
        var placementTex = AssetDatabase.LoadAssetAtPath<Texture2D>(placementPath);
        if (rangeTex != null)
            asset.rangeRingTexture = rangeTex;
        if (placementTex != null)
            asset.placementTexture = placementTex;

        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
