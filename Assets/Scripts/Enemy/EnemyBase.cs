using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] protected int maxHealth = 2;
    //[SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected int scoreValue = 100;
    [SerializeField] protected int damageToPlayer = 1;

    protected int currentHealth;
    protected bool isDead = false;

    private float destroyYBottom;
    private float destroyXLimit;

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;
        destroyYBottom = -camHeight * 1.1f;     // Distruggi quando è un po' sotto la camera
        destroyXLimit = camWidth * 1.1f;    // Distruggi quando è un po' oltre i bordi laterali
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // Distruggi se fuori dai bordi camera
        if (transform.position.y < destroyYBottom ||
            Mathf.Abs(transform.position.x) > destroyXLimit)
        {
            Destroy(gameObject);
            return;
        }

        UpdateBehavior();
    }

    // METODO ASTRATTO: ogni nemico DEVE implementarlo
    protected abstract void UpdateBehavior();

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayAsteroidExplode(); // TODO: riusa suono esistente, sostituire con nemico-specifico in futuro

        OnDamageFeedback();

        if (currentHealth <= 0)
            Die();
    }

    protected virtual void OnDamageFeedback()
    {
        // Base: piccola esplosione sulla posizione del nemico
        if (ExplosionManager.Instance != null)
            ExplosionManager.Instance.SpawnSmall(
                new Vector3(transform.position.x, transform.position.y, -1f), 0.7f);
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        //// Esplosione di morte
        //if (ExplosionManager.Instance != null)
        //    ExplosionManager.Instance.SpawnBig(
        //        new Vector3(transform.position.x, transform.position.y, -1f), 1.2f);

        // Punteggio
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(scoreValue);

        // Suono
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayAsteroidExplode();    // TODO suono dedicato per nemico

        Destroy(gameObject, 0.1f); // piccolo delay per far partire l'esplosione
    }

    // Collisione con proiettile player o con la nave
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("Bullet"))
        {
            TakeDamage(1);
            Destroy(collision.gameObject);
        }

        if (collision.CompareTag("Player"))
        {
            // Danneggia il player
            PlayerHealth ph = collision.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damageToPlayer);

            // Il nemico muore al contatto (puoi overridare per i bomber che sopravvivono)
            Die();
        }
    }
}