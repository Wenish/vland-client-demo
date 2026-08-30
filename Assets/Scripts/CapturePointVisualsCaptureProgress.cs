using ShadowInfection;
using ShadowInfection.DI;
using UnityEngine;
using UnityEngine.VFX;

public class CapturePointVisualsCaptureProgress : MonoBehaviour
{
    [SerializeField]
    private CapturePointController capturePointController;

    [SerializeField]
    private VisualEffect captureProgressEffect;

    private static readonly int ProgressId = Shader.PropertyToID("CaptureProgress");
    private static readonly int TeamColorId = Shader.PropertyToID("TeamColor");

    private ITeamColorService teamColorService;

    private void Awake()
    {
        GameLifetimeScope.TryResolve(out teamColorService);
    }

    private void OnEnable()
    {
        if (capturePointController != null)
        {
            capturePointController.OnCaptureProgressChanged += HandleCaptureProgressChanged;
            capturePointController.OnContenderTeamChanged += HandleContenderTeamChanged;
            UpdateVisualEffect(capturePointController.captureProgress, capturePointController.contenderTeam);
        }
    }

    private void OnDisable()
    {
        if (capturePointController != null)
        {
            capturePointController.OnCaptureProgressChanged -= HandleCaptureProgressChanged;
            capturePointController.OnContenderTeamChanged -= HandleContenderTeamChanged;
        }
    }

    private void HandleCaptureProgressChanged(float newProgress)
    {
        UpdateVisualEffect(newProgress, capturePointController.contenderTeam);
    }

    private void HandleContenderTeamChanged((int oldTeam, int newTeam) teamChange)
    {
        UpdateVisualEffect(capturePointController.captureProgress, teamChange.newTeam);
    }

    private void UpdateVisualEffect(float progress, int teamId)
    {
        if (captureProgressEffect == null)
        {
            return;
        }

        if (teamId < 0)
        {
            captureProgressEffect.SetFloat(ProgressId, 0f);
            return;
        }

        if (teamColorService == null)
        {
            GameLifetimeScope.TryResolve(out teamColorService);
        }

        captureProgressEffect.SetFloat(ProgressId, progress);

        if (teamColorService == null)
        {
            return;
        }

        Color teamColor = teamColorService.GetColorForTeam(teamId);
        teamColor.a = 1f; // Ensure full opacity for the effect
        captureProgressEffect.SetVector4(TeamColorId, teamColor);
    }
}