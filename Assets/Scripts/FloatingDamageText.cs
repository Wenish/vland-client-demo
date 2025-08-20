using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    private Camera cameraToLookAt;
    private float moveSpeed = 1f;
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

    public void Initialize(string text, Vector3 worldOffset, Color color, float fontSize)
    {
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        offset = worldOffset;
        transform.position += offset;
        textColor = color;
        textMesh.color = textColor;
        moveSpeed = Random.Range(1f, 1.4f); // Set a random move speed between 0.5 and 2
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move upwards and fade out
        Vector3 direction = transform.position - cameraToLookAt.transform.position;
        direction.y = 0; // Ignore vertical movement for left/right calculation
        float horizontalDirection = Vector3.Dot(cameraToLookAt.transform.right, direction.normalized);
        transform.position += new Vector3(horizontalDirection * moveSpeed * Time.deltaTime, moveSpeed * Time.deltaTime, 0);
        textColor.a -= 1f / fadeSpeed * Time.deltaTime;
        textMesh.color = textColor;
    }
    
    private void LookAtCamera()
    {
        transform.LookAt(transform.position + cameraToLookAt.transform.rotation * Vector3.forward, cameraToLookAt.transform.rotation * Vector3.up);
    }
}