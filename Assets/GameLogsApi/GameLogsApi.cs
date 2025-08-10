using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Unity out-of-the-box version (no System.Text.Json / no Newtonsoft).
/// Uses UnityEngine.JsonUtility and a tiny manual JSON builder for cases
/// where we need to embed a raw JSON payload.
///
/// Notes:
/// - JsonUtility handles simple POCOs with public fields. It does not
///   support dictionaries/polymorphism well. For complex payloads, prefer
///   sending a raw JSON string via LogEventJson(...).
/// - For IL2CPP/AOT & stripping, this works with default settings because
///   JsonUtility is Unity-native.
/// </summary>
public sealed class GameLogsApi : MonoBehaviour
{
    // ===== Singleton =====
    private static GameLogsApi _instance;
    public static GameLogsApi Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindFirstInstanceInScene();
            if (_instance != null) return _instance;
            var go = new GameObject(nameof(GameLogsApi));
            _instance = go.AddComponent<GameLogsApi>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    private static GameLogsApi FindFirstInstanceInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<GameLogsApi>();
#else
        return FindObjectOfType<GameLogsApi>();
#endif
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // ===== Config =====
    [Header("API")]
    [SerializeField] private string baseUrl = "http://localhost:3000";
    [SerializeField] private string apiKey = "";
    [SerializeField] private int timeoutSeconds = 15;
    [SerializeField] private int maxRetries = 2;
    [SerializeField] private int initialBackoffMs = 300;

    public GameLogsApi SetBaseUrl(string url) { if (!string.IsNullOrWhiteSpace(url)) baseUrl = url; return this; }
    public GameLogsApi SetApiKey(string key) { apiKey = key ?? ""; return this; }
    public GameLogsApi SetTimeout(int seconds) { timeoutSeconds = Mathf.Max(1, seconds); return this; }
    public GameLogsApi SetRetries(int retries, int initialBackoffMilliseconds = 300)
    {
        maxRetries = Mathf.Max(0, retries);
        initialBackoffMs = Mathf.Max(50, initialBackoffMilliseconds);
        return this;
    }
    public GameLogsApi Configure(string url, string key, int timeoutSec = 15) =>
        SetBaseUrl(url).SetApiKey(key).SetTimeout(timeoutSec);

    // ===== DTOs =====
    [Serializable] public class SessionResource { public string id; public string playerId; public string startedAt; public string endedAt; }

    // NOTE: JsonUtility cannot map an arbitrary JSON object into a field.
    // We expose payload as raw JSON text. If you need a typed payload, parse
    // it separately in game code with your own model using JsonUtility.
    [Serializable] public class EventResource
    {
        public string id;
        public string sessionId;
        public string ts;
        public string type;
        // Raw JSON for the payload; may be null if server omitted it or mapping failed.
        public string payloadRaw;
    }

    // When sending, prefer LogEventJson for complex payloads.
    [Serializable] private class CreateEventBody
    {
        public string type;
        // We do not include a payload field here because we need to inject
        // raw JSON without quotes. We build the JSON manually.
    }

    public class ApiResult<T>
    {
        public bool Success;
        public long Status;
        public T Data;
        public string Error;
        public static ApiResult<T> Ok(T data, long status) => new ApiResult<T> { Success = true, Data = data, Status = status };
        public static ApiResult<T> Fail(string error, long status) => new ApiResult<T> { Success = false, Error = error, Status = status };
    }

    // ===== Public API =====
    public Task<ApiResult<SessionResource>> StartSession(string playerExternalId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(playerExternalId))
            return Task.FromResult(ApiResult<SessionResource>.Fail("playerExternalId required", 0));
        var url = $"{TrimSlash(baseUrl)}/sessions/{UnityWebRequest.EscapeURL(playerExternalId)}/start";
        return SendAsync<SessionResource>(url, UnityWebRequest.kHttpVerbPOST, null, ct);
    }

    public Task<ApiResult<SessionResource>> EndSession(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(ApiResult<SessionResource>.Fail("sessionId required", 0));
        var url = $"{TrimSlash(baseUrl)}/sessions/{UnityWebRequest.EscapeURL(sessionId)}/end";
        return SendAsync<SessionResource>(url, UnityWebRequest.kHttpVerbPOST, null, ct);
    }

    /// <summary>
    /// Log an event with a POCO payload that JsonUtility can serialize.
    /// For dictionaries or polymorphic objects, use LogEventJson and pass a raw JSON string.
    /// </summary>
    public Task<ApiResult<EventResource>> LogEvent(string sessionId, string type, object payload = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(ApiResult<EventResource>.Fail("sessionId required", 0));
        if (string.IsNullOrWhiteSpace(type))
            return Task.FromResult(ApiResult<EventResource>.Fail("type required", 0));

        var url = $"{TrimSlash(baseUrl)}/sessions/{UnityWebRequest.EscapeURL(sessionId)}/events";

        // If payload is a raw JSON string, use it as-is. Otherwise try JsonUtility.ToJson.
        string payloadJson = null;
        if (payload is string s && LooksLikeJson(s))
        {
            payloadJson = s;
        }
        else if (payload != null)
        {
            try { payloadJson = JsonUtility.ToJson(payload); }
            catch { payloadJson = null; }
        }

        string bodyJson = BuildCreateEventJson(type, payloadJson);
        return SendAsync<EventResource>(url, UnityWebRequest.kHttpVerbPOST, bodyJson, ct, bodyIsRawJson: true);
    }

    /// <summary>
    /// Log an event with a raw JSON payload string (e.g., "{\"score\":10}").
    /// This is the safest path for complex data without extra packages.
    /// </summary>
    public Task<ApiResult<EventResource>> LogEventJson(string sessionId, string type, string payloadJson = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(ApiResult<EventResource>.Fail("sessionId required", 0));
        if (string.IsNullOrWhiteSpace(type))
            return Task.FromResult(ApiResult<EventResource>.Fail("type required", 0));

        var url = $"{TrimSlash(baseUrl)}/sessions/{UnityWebRequest.EscapeURL(sessionId)}/events";
        string bodyJson = BuildCreateEventJson(type, payloadJson);
        return SendAsync<EventResource>(url, UnityWebRequest.kHttpVerbPOST, bodyJson, ct, bodyIsRawJson: true);
    }

    // ===== Core request logic =====
    private async Task<ApiResult<T>> SendAsync<T>(string url, string method, object body, CancellationToken ct, bool bodyIsRawJson = false)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return ApiResult<T>.Fail("Base URL not configured", 0);

        int attempt = 0;
        int backoff = initialBackoffMs;

        while (true)
        {
            attempt++;
            UnityWebRequest req = null;
            CancellationTokenRegistration ctr = default;

            try
            {
                req = new UnityWebRequest(url, method);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = timeoutSeconds;

                if (body != null)
                {
                    string json;
                    try
                    {
                        if (bodyIsRawJson && body is string raw)
                        {
                            json = raw;
                        }
                        else
                        {
                            // Fallback: try to serialize a POCO with JsonUtility.
                            json = JsonUtility.ToJson(body);
                        }
                    }
                    catch (Exception ex)
                    {
                        return ApiResult<T>.Fail("Serialization error: " + ex.Message, 0);
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    req.uploadHandler = new UploadHandlerRaw(bytes);
                    req.SetRequestHeader("Content-Type", "application/json");
                }

                if (!string.IsNullOrEmpty(apiKey))
                    req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                if (ct.CanBeCanceled)
                {
                    ctr = ct.Register(() =>
                    {
                        try { if (req != null && !req.isDone) req.Abort(); } catch { }
                    });
                }

                var op = req.SendWebRequest();
                await WaitAsync(op, ct);

                long status = req.responseCode;
                string text = req.downloadHandler != null ? req.downloadHandler.text : null;

                if (status >= 200 && status < 300)
                {
                    if (typeof(T) == typeof(string))
                        return ApiResult<T>.Ok((T)(object)(text ?? ""), status);

                    if (string.IsNullOrEmpty(text))
                        return ApiResult<T>.Ok(default, status);

                    try
                    {
                        // If T is EventResource, attempt to capture payload as raw JSON.
                        if (typeof(T) == typeof(EventResource))
                        {
                            var parsed = TryMapEventResource(text, out var evt);
                            if (!parsed)
                                return ApiResult<T>.Fail("JSON parse error for EventResource", status);
                            return ApiResult<T>.Ok((T)(object)evt, status);
                        }

                        var data = JsonUtility.FromJson<T>(text);
                        return ApiResult<T>.Ok(data, status);
                    }
                    catch (Exception ex)
                    {
                        return ApiResult<T>.Fail($"JSON parse error: {ex.Message}\nBody: {Truncate(text, 500)}", status);
                    }
                }

                bool transient = (status >= 500 && status < 600) ||
                                 req.result == UnityWebRequest.Result.ConnectionError ||
                                 status == 0;

                if (transient && attempt <= maxRetries && !ct.IsCancellationRequested)
                {
                    int jitter = UnityEngine.Random.Range(0, backoff / 2);
                    try { await Task.Delay(backoff + jitter, ct); } catch { }
                    backoff = Mathf.Min(backoff * 2, 30_000);
                    continue;
                }

                var baseErr = !string.IsNullOrWhiteSpace(req.error) ? req.error : null;
                var bodyErr = !string.IsNullOrWhiteSpace(text) ? Truncate(text, 500) : null;
                string combined = baseErr != null && bodyErr != null
                    ? baseErr + "\n" + bodyErr
                    : (baseErr ?? bodyErr ?? "Request failed");
                return ApiResult<T>.Fail(combined, status);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<T>.Fail("Cancelled", -1);
            }
            catch (Exception ex)
            {
                if (attempt <= maxRetries && !ct.IsCancellationRequested)
                {
                    int jitter = UnityEngine.Random.Range(0, backoff / 2);
                    try { await Task.Delay(backoff + jitter, ct); } catch { }
                    backoff = Mathf.Min(backoff * 2, 30_000);
                    continue;
                }
                return ApiResult<T>.Fail("Exception: " + ex.Message, 0);
            }
            finally
            {
                try { ctr.Dispose(); } catch { }
                if (req != null) req.Dispose();
            }
        }
    }

    // Await UnityWebRequest without busy loop
    private static Task WaitAsync(UnityWebRequestAsyncOperation op, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        op.completed += _ => tcs.TrySetResult(true);
        if (ct.CanBeCanceled)
        {
            ct.Register(() => { tcs.TrySetCanceled(ct); });
        }
        return tcs.Task;
    }

    // ===== Helpers =====
    private static string TrimSlash(string s) => string.IsNullOrEmpty(s) ? s : s.TrimEnd('/');
    private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "…";
    private static bool LooksLikeJson(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        return (s.Length >= 2 && ((s[0] == '{' && s[s.Length - 1] == '}') || (s[0] == '[' && s[s.Length - 1] == ']')));
    }

    private static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    // Build {"type":"...","payload":<raw-or-empty>}
    private static string BuildCreateEventJson(string type, string payloadJson)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"type\":\"").Append(JsonEscape(type ?? "")).Append("\"");
        sb.Append(',');
        sb.Append("\"payload\":");
        if (LooksLikeJson(payloadJson))
            sb.Append(payloadJson);
        else
            sb.Append("{}");
        sb.Append('}');
        return sb.ToString();
    }

    // Minimal extraction for EventResource to capture payload as raw JSON
    // without a full JSON library. Assumes a flat object with fields we need.
    // If the JSON structure changes significantly, consider returning string
    // instead and parsing upstream.
    private static bool TryMapEventResource(string json, out EventResource evt)
    {
        evt = null;
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            // First, map everything except payload with JsonUtility.
            // Create a shadow class that matches server fields except payload.
            var shadow = JsonUtility.FromJson<EventShadow>(json);
            if (shadow == null) return false;

            // Extract raw payload text by locating the "payload": token.
            string payloadRaw = TryExtractPayloadRaw(json);

            evt = new EventResource
            {
                id = shadow.id,
                sessionId = shadow.sessionId,
                ts = shadow.ts,
                type = shadow.type,
                payloadRaw = payloadRaw
            };
            return true;
        }
        catch { return false; }
    }

    [Serializable]
    private class EventShadow { public string id; public string sessionId; public string ts; public string type; }

    private static string TryExtractPayloadRaw(string json)
    {
        // Very small, lenient finder for a top-level \"payload\": value.
        // It does not fully parse JSON; it scans for the token and then
        // balances braces/brackets to capture the raw value.
        const string key = "\"payload\"";
        int i = json.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return null;
        i += key.Length;
        // Skip whitespace and colon
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length || json[i] != ':') return null;
        i++; // past ':'
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length) return null;

        char c = json[i];
        if (c == '"')
        {
            // String payload – capture quoted string
            int start = i + 1; i++;
            var sb = new StringBuilder();
            bool escape = false;
            while (i < json.Length)
            {
                char ch = json[i++];
                if (escape) { sb.Append(ch); escape = false; continue; }
                if (ch == '\\') { escape = true; continue; }
                if (ch == '"') break;
                sb.Append(ch);
            }
            // Re-wrap as JSON string
            return '"' + sb.ToString() + '"';
        }
        else if (c == '{' || c == '[')
        {
            // Object or array – balance braces/brackets
            int start = i;
            int depth = 0;
            for (; i < json.Length; i++)
            {
                char ch = json[i];
                if (ch == '"')
                {
                    // skip strings
                    i++;
                    bool esc = false;
                    while (i < json.Length)
                    {
                        char ch2 = json[i++];
                        if (esc) { esc = false; continue; }
                        if (ch2 == '\\') { esc = true; continue; }
                        if (ch2 == '"') break;
                    }
                    i--; // step back one because for-loop will ++
                    continue;
                }
                if (ch == '{' || ch == '[') depth++;
                else if (ch == '}' || ch == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        int end = i + 1;
                        return json.Substring(start, end - start);
                    }
                }
            }
        }
        else
        {
            // primitives: true, false, null, numbers
            int start = i;
            while (i < json.Length)
            {
                char ch = json[i];
                if (ch == ',' || ch == '}' || ch == ']') break;
                i++;
            }
            return json.Substring(start, i - start).Trim();
        }
        return null;
    }
}
