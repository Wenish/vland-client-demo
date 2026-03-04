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

    private TeamColorManager teamColorManager;

    private void Awake()
    {
        teamColorManager = TeamColorManager.Instance;
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
        Debug.Log($"Capture progress changed: {newProgress}");
        UpdateVisualEffect(newProgress, capturePointController.contenderTeam);
    }

    private void HandleContenderTeamChanged((int oldTeam, int newTeam) teamChange)
    {
        Debug.Log($"Contender team changed: {teamChange.oldTeam} -> {teamChange.newTeam}");
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

        if (teamColorManager == null)
        {
            teamColorManager = TeamColorManager.Instance;
        }

        captureProgressEffect.SetFloat(ProgressId, progress);

        Color teamColor = teamColorManager.GetColorForTeam(teamId);
        teamColor.a = 1f; // Ensure full opacity for the effect
        captureProgressEffect.SetVector4(TeamColorId, teamColor);
    }
}