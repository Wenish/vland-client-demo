using UnityEngine;

public class TextBillboard : MonoBehaviour
{
    private static Camera cachedCamera;

    void Awake()
    {
        // Cache the main camera once, if not already cached
        if (cachedCamera == null)
            cachedCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (cachedCamera == null)
            return;

        // Make the text face the camera directly
        transform.forward = cachedCamera.transform.forward;
    }
}