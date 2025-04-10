using UnityEngine;

public class FloatingTextMotion : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.25f; // Height of the motion
    [SerializeField] private float frequency = 1f;     // Speed of the motion

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = initialPosition + Vector3.up * offset;
    }
}