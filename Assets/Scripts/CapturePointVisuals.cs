using ShadowInfection;
using ShadowInfection.DI;
using UnityEngine;

public class CapturePointVisuals : MonoBehaviour
{
    [SerializeField] private CapturePointController capturePoint;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private ITeamColorService teamColorService;
    private MaterialPropertyBlock materialPropertyBlock;
    private int activeColorPropertyId = BaseColorId;

    [Header("Visual Settings")]
    [Tooltip("Opacity of Color")]
    [SerializeField]
    private float colorOpacity = 0.5f;

    [Tooltip("Color used when the capture point is neutral (-1).")]
    [SerializeField]
    private Color neutralColor = new(0.5f, 0.5f, 0.5f, 1f);


    [SerializeField]
    private MeshRenderer meshRenderer;


    private void OnEnable()
    {
        if (capturePoint != null)
        {
            capturePoint.OnControllingTeamChanged += HandleCapturePointUpdated;
            ApplyTeamColor(capturePoint.controllingTeam);
        }
    }

    private void OnDisable()
    {
        if (capturePoint != null)
        {
            capturePoint.OnControllingTeamChanged -= HandleCapturePointUpdated;
        }
    }

    private void HandleCapturePointUpdated((int oldTeam, int newTeam) teamChange)
    {
        ApplyTeamColor(teamChange.newTeam);
    }

    private void ApplyTeamColor(int teamId)
    {
        if (meshRenderer == null)
        {
            return;
        }

        if (teamColorService == null)
        {
            GameLifetimeScope.TryResolve(out teamColorService);
        }

        Color color;
        if (teamId < 0 || teamColorService == null)
        {
            color = neutralColor;
        }
        else
        {
            color = teamColorService.GetColorForTeam(teamId);
        }

        color.a = Mathf.Clamp01(colorOpacity);

        materialPropertyBlock ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor(activeColorPropertyId, color);
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void OnValidate()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        colorOpacity = Mathf.Clamp01(colorOpacity);
    }

    private void Awake()
    {
        GameLifetimeScope.TryResolve(out teamColorService);
        materialPropertyBlock = new MaterialPropertyBlock();

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshRenderer != null)
        {
            activeColorPropertyId = ResolveColorPropertyId(meshRenderer);
        }

        if (capturePoint == null)
        {
            Debug.LogError("CapturePoint reference is missing on CapturePointVisuals.", this);
        }
        if (teamColorService == null)
        {
            Debug.LogError("ITeamColorService is not registered.", this);
        }
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer reference is missing on CapturePointVisuals.", this);
        }
    }

    private static int ResolveColorPropertyId(Renderer renderer)
    {
        if (renderer == null)
        {
            return BaseColorId;
        }

        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            return BaseColorId;
        }

        if (material.HasProperty(BaseColorId))
        {
            return BaseColorId;
        }

        if (material.HasProperty(LegacyColorId))
        {
            return LegacyColorId;
        }

        return BaseColorId;
    }
}