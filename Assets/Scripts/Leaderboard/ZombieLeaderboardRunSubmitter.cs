using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mirror;
using MyGame.Events;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-850)]
[DisallowMultipleComponent]
public sealed class ZombieLeaderboardRunSubmitter : MonoBehaviour
{
    private const string DefaultBaseUrl = "https://leaderboard-api.shadowinfection.com";
    private const int MaxPlayersPerRun = 5;
    private static readonly Regex UnsafeNicknameChars = new Regex("[^a-zA-Z0-9 _\\-]", RegexOptions.Compiled);
    private static ZombieLeaderboardRunSubmitter instance;

    [SerializeField] private bool isSubmissionEnabled = true;
    [SerializeField] private string baseUrl = DefaultBaseUrl;
    [SerializeField] private int timeoutSeconds = 10;
    [SerializeField] private int maxRetryAttempts = 3;
    [SerializeField] private float initialRetryDelaySeconds = 1.5f;

    private bool isSubscribed;
    private bool isProcessingQueue;
    private readonly Queue<QueuedSubmission> pendingSubmissions = new Queue<QueuedSubmission>();

    private int runSequence;
    private int activeRunSequence = -1;
    private bool hasSubmittedForActiveRun;
    private float fallbackRunStartTime;
    private int nextSubmissionId = 1;

    private CancellationTokenSource cts;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrapInstance()
    {
        var existing = FindAnyObjectByType<ZombieLeaderboardRunSubmitter>();
        if (existing != null)
        {
            return;
        }

        var host = new GameObject(nameof(ZombieLeaderboardRunSubmitter));
        host.AddComponent<ZombieLeaderboardRunSubmitter>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        cts = new CancellationTokenSource();
    }

    private void Update()
    {
        TrySubscribe();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        Unsubscribe();

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    private void TrySubscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (EventManager.Instance == null)
        {
            return;
        }

        EventManager.Instance.Subscribe<ZombieGameOverEvent>(HandleZombieGameOverEvent);
        EventManager.Instance.Subscribe<ZombieRunEndedEvent>(HandleZombieRunEndedEvent);
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || EventManager.Instance == null)
        {
            return;
        }

        EventManager.Instance.Unsubscribe<ZombieGameOverEvent>(HandleZombieGameOverEvent);
        EventManager.Instance.Unsubscribe<ZombieRunEndedEvent>(HandleZombieRunEndedEvent);
        isSubscribed = false;
    }

    private void HandleZombieGameOverEvent(ZombieGameOverEvent gameOverEvent)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        if (!gameOverEvent.IsGameOver)
        {
            BeginRun();
            return;
        }

        CaptureAndQueueIfNeeded(ZombieRunEndReason.AllPlayersDead);
    }

    private void HandleZombieRunEndedEvent(ZombieRunEndedEvent runEndedEvent)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        CaptureAndQueueIfNeeded(runEndedEvent.EndReason);
    }

    private void BeginRun()
    {
        activeRunSequence = ++runSequence;
        hasSubmittedForActiveRun = false;
        fallbackRunStartTime = Time.time;
    }

    private void EnsureRunInitialized()
    {
        if (activeRunSequence >= 0)
        {
            return;
        }

        BeginRun();
    }

    private void CaptureAndQueueIfNeeded(ZombieRunEndReason reason)
    {
        if (!isSubmissionEnabled)
        {
            return;
        }

        EnsureRunInitialized();
        if (hasSubmittedForActiveRun)
        {
            return;
        }

        if (!TryBuildSubmissionPayload(out var payload, out var payloadError))
        {
            Debug.LogWarning($"[{nameof(ZombieLeaderboardRunSubmitter)}] Skipping leaderboard submit: {payloadError}", this);
            return;
        }

        hasSubmittedForActiveRun = true;

        var queueItem = new QueuedSubmission
        {
            SubmissionId = nextSubmissionId++,
            RunSequence = activeRunSequence,
            EndReason = reason,
            Payload = payload
        };

        pendingSubmissions.Enqueue(queueItem);
        if (!isProcessingQueue)
        {
            _ = ProcessQueueAsync();
        }
    }

    private bool TryBuildSubmissionPayload(out ZombieRunSubmitDto payload, out string error)
    {
        payload = null;
        error = null;

        var gameManager = ZombieGameManager.Singleton;
        if (gameManager == null)
        {
            error = "ZombieGameManager not available.";
            return false;
        }

        var players = new List<ZombieRunPlayerDto>();
        int totalPoints = 0;

        var entries = gameManager.LeaderboardEntries;
        if (entries == null)
        {
            error = "Leaderboard entries unavailable.";
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var row = entries[i];
            if (row.ConnectionId < 0)
            {
                continue;
            }

            string nickname = string.IsNullOrWhiteSpace(row.PlayerName)
                ? $"Player{row.ConnectionId}"
                : row.PlayerName.Trim();

            nickname = SanitizeNickname(nickname, row.ConnectionId);

            if (nickname.Length > 32)
            {
                nickname = nickname.Substring(0, 32);
            }

            int points = Mathf.Max(0, row.Points);
            players.Add(new ZombieRunPlayerDto
            {
                nickname = nickname,
                points = points,
                zombieKills = Mathf.Max(0, row.Kills),
                goldCollected = Mathf.Max(0, row.GoldGathered),
                deaths = Mathf.Max(0, row.Deaths)
            });
            totalPoints += points;

            if (players.Count >= MaxPlayersPerRun)
            {
                break;
            }
        }

        if (players.Count == 0)
        {
            error = "No human players found for submission payload.";
            return false;
        }

        float runStart = gameManager.RunStartedAtServerTime > 0f
            ? gameManager.RunStartedAtServerTime
            : fallbackRunStartTime;

        int durationSeconds = Mathf.RoundToInt(Mathf.Max(0f, Time.time - runStart));

        payload = new ZombieRunSubmitDto
        {
            version = NormalizeVersion(ZombieLeaderboardVersionResolver.ResolveGameVersion()),
            playerCount = players.Count,
            wavesSurvived = Mathf.Max(0, gameManager.CurrentWave),
            duration = durationSeconds,
            totalPoints = Mathf.Max(0, totalPoints),
            players = players
        };

        return true;
    }

    private async Task ProcessQueueAsync()
    {
        if (isProcessingQueue)
        {
            return;
        }

        isProcessingQueue = true;

        try
        {
            while (pendingSubmissions.Count > 0)
            {
                if (cts == null || cts.IsCancellationRequested)
                {
                    return;
                }

                var item = pendingSubmissions.Dequeue();
                bool delivered = false;
                int attempt = 0;

                while (!delivered && attempt <= maxRetryAttempts)
                {
                    if (cts.IsCancellationRequested)
                    {
                        return;
                    }

                    attempt++;
                    var result = await SubmitZombieRunAsync(item.Payload, cts.Token);
                    if (result.Success)
                    {
                        delivered = true;
                        Debug.Log($"[{nameof(ZombieLeaderboardRunSubmitter)}] Submitted run #{item.SubmissionId} (runSeq={item.RunSequence}, reason={item.EndReason}).", this);
                        break;
                    }

                    bool canRetry = result.IsTransient && attempt <= maxRetryAttempts;
                    if (!canRetry)
                    {
                        Debug.LogWarning($"[{nameof(ZombieLeaderboardRunSubmitter)}] Dropped run #{item.SubmissionId} after {attempt} attempt(s). Status={result.Status}, Error={result.Error}", this);
                        break;
                    }

                    float retryDelay = initialRetryDelaySeconds * Mathf.Pow(2f, attempt - 1);
                    retryDelay += UnityEngine.Random.Range(0f, 0.5f);
                    int retryDelayMs = Mathf.RoundToInt(Mathf.Max(0.1f, retryDelay) * 1000f);

                    Debug.LogWarning($"[{nameof(ZombieLeaderboardRunSubmitter)}] Retry {attempt}/{maxRetryAttempts} for run #{item.SubmissionId} in {retryDelay:0.0}s. Status={result.Status}, Error={result.Error}", this);
                    await Task.Delay(retryDelayMs, cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            isProcessingQueue = false;
        }
    }

    private async Task<SubmissionResult> SubmitZombieRunAsync(ZombieRunSubmitDto payload, CancellationToken token)
    {
        string endpoint = $"{TrimSlash(baseUrl)}/runs/zombie";

        UnityWebRequest request = null;
        CancellationTokenRegistration cancellationRegistration = default;

        try
        {
            string json = JsonUtility.ToJson(payload);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);

            request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.Max(1, timeoutSeconds);

            if (token.CanBeCanceled)
            {
                cancellationRegistration = token.Register(() =>
                {
                    try
                    {
                        if (request != null && !request.isDone)
                        {
                            request.Abort();
                        }
                    }
                    catch
                    {
                    }
                });
            }

            var operation = request.SendWebRequest();
            await WaitForRequestAsync(operation, token);

            long status = request.responseCode;
            bool success = status >= 200 && status < 300;
            if (success)
            {
                return SubmissionResult.Ok(status);
            }

            bool transient = status == 0
                || (status >= 500 && status < 600)
                || request.result == UnityWebRequest.Result.ConnectionError;

            string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            string error = BuildHttpError(request.error, responseBody);

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                Debug.LogWarning($"[{nameof(ZombieLeaderboardRunSubmitter)}] Server rejected payload with status {status}. Body={Truncate(responseBody, 800)} Payload={JsonUtility.ToJson(payload)}", this);
            }

            return SubmissionResult.Fail(status, error, transient);
        }
        catch (OperationCanceledException)
        {
            return SubmissionResult.Fail(-1, "Submission cancelled.", true);
        }
        catch (Exception ex)
        {
            return SubmissionResult.Fail(0, ex.Message, true);
        }
        finally
        {
            try
            {
                cancellationRegistration.Dispose();
            }
            catch
            {
            }

            if (request != null)
            {
                request.Dispose();
            }
        }
    }

    private static Task WaitForRequestAsync(UnityWebRequestAsyncOperation operation, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        operation.completed += _ => tcs.TrySetResult(true);
        if (token.CanBeCanceled)
        {
            token.Register(() => tcs.TrySetCanceled(token));
        }

        return tcs.Task;
    }

    private static string TrimSlash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultBaseUrl : value.TrimEnd('/');
    }

    private static string SanitizeNickname(string rawNickname, int connectionId)
    {
        string nickname = string.IsNullOrWhiteSpace(rawNickname) ? $"Player{connectionId}" : rawNickname.Trim();
        nickname = UnsafeNicknameChars.Replace(nickname, string.Empty);

        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = $"Player{connectionId}";
        }

        return nickname;
    }

    private static string NormalizeVersion(string rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return "0.0.0";
        }

        string sanitized = rawVersion.Trim();
        if (sanitized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = sanitized.Substring(1);
        }

        var match = Regex.Match(sanitized, "\\d+\\.\\d+\\.\\d+");
        return match.Success ? match.Value : "0.0.0";
    }

    private static string BuildHttpError(string requestError, string responseBody)
    {
        string left = string.IsNullOrWhiteSpace(requestError) ? string.Empty : requestError.Trim();
        string right = string.IsNullOrWhiteSpace(responseBody) ? string.Empty : Truncate(responseBody, 800).Trim();

        if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right))
        {
            return $"{left} | {right}";
        }

        return !string.IsNullOrWhiteSpace(left) ? left : (!string.IsNullOrWhiteSpace(right) ? right : "Request failed.");
    }

    private static string Truncate(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value) || maxLen <= 0)
        {
            return string.Empty;
        }

        return value.Length <= maxLen ? value : value.Substring(0, maxLen);
    }

    private struct QueuedSubmission
    {
        public int SubmissionId;
        public int RunSequence;
        public ZombieRunEndReason EndReason;
        public ZombieRunSubmitDto Payload;
    }

    private struct SubmissionResult
    {
        public bool Success;
        public long Status;
        public string Error;
        public bool IsTransient;

        public static SubmissionResult Ok(long status)
        {
            return new SubmissionResult
            {
                Success = true,
                Status = status,
                Error = string.Empty,
                IsTransient = false
            };
        }

        public static SubmissionResult Fail(long status, string error, bool isTransient)
        {
            return new SubmissionResult
            {
                Success = false,
                Status = status,
                Error = string.IsNullOrWhiteSpace(error) ? "Request failed." : error,
                IsTransient = isTransient
            };
        }
    }
}
