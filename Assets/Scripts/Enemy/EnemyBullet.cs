using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 24f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 4f; // Distruggi dopo 5 secondi se non colpisce nulla 
    // TODO: calcolare un limite di distanza dalla camera e distruggerlo se supera quel limite, invece di usare un timer, per evitare che i proiettili "fantasma" continuino a esistere fuori dalla vista del giocatore.

    private float destroyYBottom;
    private float destroyYTop;
    private float destroyXLimit;

    private void Start()
    {
        //Destroy(gameObject, lifetime);
        // calcola i bordi una volta sola, con un margine generoso
        Camera cam = Camera.main;
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        destroyYBottom = -camHeight * 1.2f; // Distruggi quando è un po' sotto la camera
        destroyYTop = camHeight * 1.2f; // Distruggi quando è un po' sopra la camera (per proiettili che potrebbero rimbalzare o essere sparati verso l'alto)
        destroyXLimit = camWidth * 1.2f;    // Distruggi quando è un po' oltre i bordi laterali
    }

    private void Update()
    {
        // I FighterBullet usano Rigidbody2D con velocity diretta
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);

        // distruggi se fuori dai bordi
        if (transform.position.y < destroyYBottom ||
            transform.position.y > destroyYTop ||
            Mathf.Abs(transform.position.x) > destroyXLimit)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Colpisci il player
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }

        // distruggi il proiettile se colpisce un asteroide, ma non danneggia l'asteroide
        if (collision.CompareTag("Asteroid"))
        {
            Destroy(gameObject);    
        }
    }

    public float GetSpeed() => speed;
}