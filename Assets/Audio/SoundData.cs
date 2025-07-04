using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Game/Audio/SoundData")]
public class SoundData : ScriptableObject
{
    public string soundName;
    public AudioClip clip;
    public AudioMixerGroup mixerGroup;
    public bool isSpatial = true;

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
}