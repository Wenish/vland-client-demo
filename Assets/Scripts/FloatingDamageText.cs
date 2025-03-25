using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float lifetime = 1.5f;
    public float fadeSpeed = 2f;
    
    private TextMeshPro textMesh;
    private Color textColor;
    private Transform target;
    private Vector3 offset;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textColor = textMesh.color;
    }

    public void Initialize(int damage, Transform followTarget, Vector3 worldOffset)
    {
        textMesh.text = damage.ToString();
        target = followTarget;
        offset = worldOffset;
        // Destroy(gameObject, lifetime); // Destroy after lifetime
    }

    private void Update()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }

        // Move upwards and fade out
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);
        textColor.a -= fadeSpeed * Time.deltaTime;
        textMesh.color = textColor;
    }
}