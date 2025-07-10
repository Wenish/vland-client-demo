using System.Collections.Generic;
using UnityEngine;

public class TeamColorManager : MonoBehaviour
{
    // Singleton-Instanz
    public static TeamColorManager Instance { get; private set; }

    // Vordefinierte Farben im Inspector (z. B. für Team 0 bis 9)
    [SerializeField, ColorUsage(true, true)]
    private List<Color> predefinedColors;

    // Interne Map für bereits zugewiesene Farben
    private Dictionary<int, Color> teamColorMap = new();

    // Initialisierung
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Gibt für eine Team-ID eine Farbe zurück – konsistent.
    /// </summary>
    public Color GetColorForTeam(int teamId)
    {
        // Schon gemerkt?
        if (teamColorMap.TryGetValue(teamId, out Color color))
            return color;

        // Vordefiniert?
        if (teamId >= 0 && teamId < predefinedColors.Count)
        {
            color = predefinedColors[teamId];
        }
        else
        {
            // Generiert
            color = GenerateColorFromTeamId(teamId);
        }

        // Speichern und zurückgeben
        teamColorMap[teamId] = color;
        return color;
    }

    /// <summary>
    /// Generiert eine konsistente Farbe aus einer Team-ID
    /// (Goldener Schnitt → gute Verteilung im Farbspektrum).
    /// </summary>
    private Color GenerateColorFromTeamId(int teamId)
    {
        float hue = Mathf.Abs(teamId * 0.61803398875f) % 1f;
        float saturation = 0.7f;
        float value = 0.9f;
        return Color.HSVToRGB(hue, saturation, value);
    }
}
