using UnityEngine;

public class BossBullet : MonoBehaviour
{
    [SerializeField] private float speed = 24f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 4f; // Distruggi dopo 5 secondi se non colpisce nulla 
    // TODO: calcolare un limite di distanza dalla camera e distruggerlo se supera quel limite, invece di usare un timer, per evitare che i proiettili "fantasma" continuino a esistere fuori dalla vista del giocatore.

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Muovi verso il basso (o la direzione impostata)
        transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
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

        // Opzionale: distruggi anche se colpisce gli asteroidi o altri oggetti
        if (collision.CompareTag("Asteroid"))
        {
            Destroy(gameObject);
        }
    }
}