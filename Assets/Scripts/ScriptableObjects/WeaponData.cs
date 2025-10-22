using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    public string weaponName;
    public WeaponType weaponType;
    
    [Header("Stats")]
    public int attackPower = 10;
    public float attackRange = 5.0f;
    public float attackTime = 0.2f;
    public float attackSpeed = 1.0f;
    public float moveSpeedPercentWhileAttacking = 0.5f;

    [Header("UI")]
    public Texture2D iconTexture;

    [Header("Visuals")]
    public GameObject weaponModelRightHand;
    public GameObject weaponModelLeftHand;

    [Header("Swing VFX")]
    public List<SwingVfxListItem> swingVfxs = new List<SwingVfxListItem>();

    [Header("Audio")]
    public List<AudioListItem> onAttackStartAudioClips = new List<AudioListItem>();
    public List<AudioListItem> swingAudioClips = new List<AudioListItem>();
    public List<AudioListItem> onHitAudioClips = new List<AudioListItem>();

    public abstract void PerformAttack(UnitController attacker);

    [System.Serializable]
    public class SwingVfxListItem
    {
        public GameObject swingVfxPrefab;
        public Vector3 swingVfxPositionOffset = Vector3.zero;
        public Vector3 swingVfxRotationOffset = Vector3.zero;
        public float swingVfxLifetime = 0.3f;
    }

    [System.Serializable]
    public class AudioListItem
    {
        public SoundData soundData;

        [Range(0f, 0.5f)]
        [Tooltip("Maximum random pitch deviation (±). 0.0 = no variation, 0.5 = noticeable variation")]
        public float pitchOffset = 0f;

    }
}


public enum WeaponType : byte
{
    Unarmed,
    Sword,
    Daggers,
    Bow,
    Gun,
    Pistols
}