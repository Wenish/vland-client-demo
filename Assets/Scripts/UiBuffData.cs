using System;
using UnityEngine;

public class UiBuffData
{
    public string InstanceId; // unique per buff instance
    public string BuffId;
    public Texture2D IconTexture;
    public int StackCount = 1;
    public float Duration = Mathf.Infinity; // -1 = infinite
    public float TimeRemaining = Mathf.Infinity; // -1 = infinite
    public float NormalizedRemaining => Duration <= 0f ? 0f : Mathf.Clamp01(TimeRemaining / Duration);
}