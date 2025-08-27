using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Simple in-game wrapper around GameLogsApi.
/// - Call StartLogging() once (e.g., on scene load).
/// - Use LogEvent(...) or LogEventJson(...) during gameplay.
/// - Session is ended automatically on destroy / quit.
/// </summary>
public class GameLogManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string baseUrl = "http://localhost:3000";
    [SerializeField] private string apiKey = ""; // optional
    [SerializeField] private string playerExternalId = "player-123";

    [Header("Runtime")]
    [SerializeField] private string sessionId;
    public string SessionId { get => sessionId; private set => sessionId = value; }

    public bool isLoggingOn = true;

    CancellationTokenSource _cts;

    private async void Start()
    {
        _cts = new CancellationTokenSource();

        // configure once
        GameLogsApi.Instance
            .SetBaseUrl(baseUrl)
            .SetApiKey(apiKey)
            .SetTimeout(15)
            .SetRetries(2);

        await StartLogging();
    }

    private void Awake()
    {
        _cts = new CancellationTokenSource();

#if UNITY_EDITOR
        isLoggingOn = false;
#endif



        // load or generate persistent playerExternalId
        if (PlayerPrefs.HasKey("playerExternalId"))
        {
            playerExternalId = PlayerPrefs.GetString("playerExternalId");
        }
        else
        {
            // Get device unique string
            string rawId = SystemInfo.deviceUniqueIdentifier;

            // Compute SHA256 hash
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2")); // hex string
                playerExternalId = sb.ToString(); // 64 chars
            }

            PlayerPrefs.SetString("playerExternalId", playerExternalId);
            PlayerPrefs.Save();
        }

        // configure API
        GameLogsApi.Instance
            .SetBaseUrl(baseUrl)
            .SetApiKey(apiKey)
            .SetTimeout(15)
            .SetRetries(2);
    }

    private async void OnDestroy()
    {
        try { await EndLogging(); } catch { /* ignore */ }
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void OnApplicationQuit()
    {
        _cts?.Cancel();
    }

    public async Task StartLogging()
    {
        if (!isLoggingOn) return;

        if (string.IsNullOrWhiteSpace(playerExternalId))
        {
            Debug.LogWarning("GameLogManager: playerExternalId is empty.");
            return;
        }

        var res = await GameLogsApi.Instance.StartSession(playerExternalId, _cts.Token);
        if (!res.Success)
        {
            Debug.LogError($"StartSession failed: {res.Status} {res.Error}");
            return;
        }

        SessionId = res.Data?.id;
        Debug.Log($"GameLogManager: session started: {SessionId}");

        // optional: log an initial "session_start" event
        await LogEventJson("session_start", "{\"platform\":\"" + Application.platform + "\"}");
    }

    public async Task EndLogging()
    {
        if (string.IsNullOrEmpty(SessionId)) return;
        await LogEventJson("session_end", "{\"platform\":\"" + Application.platform + "\"}");
        var res = await GameLogsApi.Instance.EndSession(SessionId, _cts.Token);
        if (!res.Success)
        {
            Debug.LogWarning($"EndSession failed: {res.Status} {res.Error}");
            return;
        }
        Debug.Log("GameLogManager: session ended.");
        SessionId = null;
    }

    /// <summary>Log a typed payload that JsonUtility can serialize.</summary>
    public async Task LogEvent<T>(string type, T payload) where T : struct
    {
        if (string.IsNullOrEmpty(SessionId)) { Debug.LogWarning("No session; call StartLogging() first."); return; }
        var res = await GameLogsApi.Instance.LogEvent(SessionId, type, payload, _cts.Token);
        if (!res.Success) Debug.LogWarning($"LogEvent '{type}' failed: {res.Status} {res.Error}");
    }

    /// <summary>Log a raw JSON payload string (best for dictionaries or polymorphic data).</summary>
    public async Task LogEventJson(string type, string payloadJson)
    {
        if (string.IsNullOrEmpty(SessionId)) { Debug.LogWarning("No session; call StartLogging() first."); return; }
        var res = await GameLogsApi.Instance.LogEventJson(SessionId, type, payloadJson, _cts.Token);
        if (!res.Success) Debug.LogWarning($"LogEventJson '{type}' failed: {res.Status} {res.Error}");
    }
}
