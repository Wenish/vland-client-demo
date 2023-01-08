using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    // The projectile scriptable object
    public Projectile projectile;

    // The unit that shot the projectile
    public UnitController shooter;

    // The current speed of the projectile
    public float speed;

    // The current damage of the projectile
    public int damage;

    public float range;

    private Vector3 spawn;
    

    // Called when the projectile is spawned
    void OnEnable()
    {
        spawn = transform.position;
    }

    // Called every frame
    void Update()
    {
        // Move the projectile
        transform.position += transform.forward * speed * Time.deltaTime;

        var distanceTravelled = Vector3.Distance(spawn, transform.position);

         // If the projectile has travelled its range, destroy it
        if (distanceTravelled >= range)
        {
            Destroy(gameObject);
        }
    }

    // Called when the projectile collides with another collider
    void OnCollisionEnter(Collision collision)
    {
        // Get the unit controller component of the collided game object
        UnitController unit = collision.collider.GetComponent<UnitController>();

        // If the collided game object has a unit controller, deal damage to the unit
        if (unit != null && unit != shooter)
        {
            unit.TakeDamage(damage);
        }

        // Destroy the projectile
        Destroy(gameObject);
    }
}