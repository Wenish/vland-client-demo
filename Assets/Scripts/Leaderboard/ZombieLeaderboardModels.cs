using System;
using System.Collections.Generic;

[Serializable]
public class ZombieRunPlayerDto
{
    public string nickname;
    public int points;
    public int zombieKills;
    public int goldCollected;
    public int deaths;
}

[Serializable]
public class ZombieRunSubmitDto
{
    public string version;
    public int playerCount;
    public int wavesSurvived;
    public int duration;
    public int totalPoints;
    public List<ZombieRunPlayerDto> players;
}

public static class ZombieLeaderboardVersionResolver
{
    public static string ResolveGameVersion()
    {
#if UNITY_EDITOR
        return "0.0.0";
#else
        return string.IsNullOrWhiteSpace(UnityEngine.Application.version)
            ? "0.0.0"
            : UnityEngine.Application.version;
#endif
    }
}
