using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float speed = 42f;
    public int damage = 1;
    public bool piercing = false;

    private Rigidbody2D rigidBody;

    // Chiamato da WeaponData.Fire() subito dopo Instantiate, prima di Awake()
    public void Initialize(int damage, bool piercing = false)
    {
        this.damage = damage;
        this.piercing = piercing;
    }

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        // Il prefab Player_Bullet ha una rotazione base di 90° Z (sprite verso l'alto).
        // transform.right con rotazione 90° = (0, 1) = direzione verso l'alto.
        // Per proiettili angolati (SpreadGun), la rotazione di spawn determina la direzione.
        rigidBody.linearVelocity = (Vector2)transform.right * speed;
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
