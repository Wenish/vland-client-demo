using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    private Camera cameraToLookAt;
    public float moveSpeed = 10f;
    public float lifetime = .5f;
    public float fadeSpeed = 2f;
    
    private TextMeshPro textMesh;
    private Color textColor;
    private Vector3 offset;

    private void Awake()
    {
        cameraToLookAt = Camera.main;
        textMesh = GetComponent<TextMeshPro>();
        textColor = textMesh.color;
    }

    void LateUpdate()
    {
        LookAtCamera();
        float distance = Vector3.Distance(transform.position, cameraToLookAt.transform.position);
        transform.localScale = Vector3.one * distance * 0.1f; // Adjust the multiplier as needed
    }

    public void Initialize(int damage, Vector3 worldOffset, Color color)
    {
        textMesh.text = damage.ToString();
        offset = worldOffset;
        transform.position += offset;
        textColor = color;
        textMesh.color = textColor;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move upwards and fade out
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);
        textColor.a -= fadeSpeed * Time.deltaTime;
    }
    
    private void LookAtCamera()
    {
        transform.LookAt(transform.position + cameraToLookAt.transform.rotation * Vector3.forward, cameraToLookAt.transform.rotation * Vector3.up);
    }
}