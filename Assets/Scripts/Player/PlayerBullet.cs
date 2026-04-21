using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float speed = 42f;
    public int damage = 1;
    public bool piercing = false;

    private Rigidbody2D rigidBody;

    // Chiamato da WeaponData.Fire() subito dopo Instantiate, prima di Start()
    public void Initialize(int damage, bool piercing = false)
    {
        this.damage = damage;
        this.piercing = piercing;
    }

    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        // transform.up tiene conto della rotazione di spawn: usato da SpreadGun per il ventaglio
        rigidBody.linearVelocity = (Vector2)transform.up * speed;
    }

    void Update()
    {
        // Viewport check: gestisce l'uscita dalla cima e dai lati (SpreadGun)
        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);
        if (vp.x < -0.1f || vp.x > 1.1f || vp.y > 1.1f)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Asteroid"))
        {
            AsteroidHealth asteroidHealth = collision.GetComponent<AsteroidHealth>();
            if (asteroidHealth != null)
                asteroidHealth.TakeDamage(damage);

            // I proiettili perforanti (Railgun) attraversano gli asteroidi senza distruggersi
            if (!piercing) Destroy(gameObject);
        }

        // La collisione con il boss è gestita in BossBase
    }
}
