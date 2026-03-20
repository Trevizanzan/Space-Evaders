using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    public float speed = 6f; // Speed of the projectile
    private float destroyY;
    private Rigidbody2D rigidBody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float cameraHeight = Camera.main.orthographicSize * 2f;
        //float cameraWidth = cameraHeight / Screen.height * Screen.width;
        //Debug.Log($"Camera Height: {cameraHeight}");
        //Debug.Log($"Camera Width: {cameraWidth}");

        rigidBody = gameObject.GetComponent<Rigidbody2D>();
        //rigidBody.linearVelocity = transform.up * speed; // Move the projectile in the direction it is facing (up)
        rigidBody.linearVelocity = new Vector2(0, speed); // Move the projectile straight up regardless of its rotation

        // Calcola bordo superiore camera per distruzione
        float camDistance = transform.position.z - Camera.main.transform.position.z;
        Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, camDistance));
        destroyY = topRight.y + (cameraHeight * 0.2f);  // (cameraHeight * 0.1f) è un offset per distruggere il proiettile un po' dopo che esca completamente dalla camera, evitando così di avere proiettili "fantasma" che continuano a esistere fuori dalla vista del giocatore.
    }

    // Update is called once per frame
    void Update()
    {
        // Distruzione se esce sopra
        if (transform.position.y > destroyY)
        {
            Destroy(gameObject);
        }
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Asteroid"))
        {
            AsteroidHealth asteroidHealth = collision.GetComponent<AsteroidHealth>();
            if (asteroidHealth != null)
            {
                asteroidHealth.TakeDamage(1); // Applica danno all'asteroide
            }

            // Il proiettile viene distrutto sempre, in ogni caso, dopo aver colpito l'asteroide
            Destroy(gameObject);
        }
    }
}
