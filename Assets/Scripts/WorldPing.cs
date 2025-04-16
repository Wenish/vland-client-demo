using UnityEngine;

public class WorldPing : MonoBehaviour
{
    public float Lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, Lifetime);
    }
}