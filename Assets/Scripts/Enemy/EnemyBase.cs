using System.Collections;
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

    [Header("Hit Flash")]
    [SerializeField] private Material flashMaterial;   // assegna dal Inspector
    private float flashDuration = 0.1f;

    private Material originalMaterial;
    private SpriteRenderer sr;
    private Coroutine flashCoroutine;

    protected Transform playerTransform;

    protected virtual void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalMaterial = sr.material;

        currentHealth = maxHealth;

        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;
        destroyYBottom = -camHeight * 1.1f;     // Distruggi quando è un po' sotto la camera
        destroyXLimit = camWidth * 1.1f;    // Distruggi quando è un po' oltre i bordi laterali

        Spaceship ship = Spaceship.GetInstance();
        if (ship != null) playerTransform = ship.transform;
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
        //// Base: piccola esplosione sulla posizione del nemico
        //if (ExplosionManager.Instance != null)
        //    ExplosionManager.Instance.SpawnSmall(
        //        new Vector3(transform.position.x, transform.position.y, -1f), 0.7f);

        // Base: nessuna esplosione, solo suono (già gestito in TakeDamage)
        // I boss possono overridare per aggiungere feedback visivo

        FlashWhite(); // ereditato da EnemyBase/BossBase
    }

    // Chiama questo da OnDamageFeedback() in ogni nemico/boss
    protected void FlashWhite()
    {
        if (sr == null || flashMaterial == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }
    private IEnumerator FlashRoutine()
    {
        sr.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        sr.material = originalMaterial;
        flashCoroutine = null;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Esplosione solo alla morte
        if (ExplosionManager.Instance != null)
            ExplosionManager.Instance.SpawnBig(
                new Vector3(transform.position.x, transform.position.y, -1f));

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

    //______ CALCOLO BOUNDS CAMERA (regola n.3) __________________________________________
    /// <summary>
    /// Bounds della camera in world units, con offset topbar già sottratto da topY.
    /// </summary>
    public struct CameraBounds
    {
        public float minX;
        public float maxX;
        public float minY;       // bordo inferiore camera
        public float maxY;       // bordo superiore camera (lordo, senza topbar)
        public float topY;       // bordo superiore camera AL NETTO della topbar UI
        public float spawnY;     // Y di spawn (appena sopra il bordo superiore grezzo)
    }

    /// <summary>
    /// Calcola i bounds della camera una volta sola.
    /// Usa topUIPaddingViewport (stessa % usata da Spaceship) per sottrarre la topbar.
    /// </summary>
    public static CameraBounds GetCameraBounds(float topUIPaddingViewport = 0.08f, float spawnMargin = 0.5f)
    {
        Camera cam = Camera.main;
        float camDist = -cam.transform.position.z;

        Vector2 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, camDist));
        Vector2 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, camDist));

        // Calcola altezza topbar in world units (identico a Spaceship.RecalculateBounds)
        float uiTopY = cam.ViewportToWorldPoint(new Vector3(0f, 1f, camDist)).y;
        float uiBottomY = cam.ViewportToWorldPoint(new Vector3(0f, 1f - topUIPaddingViewport, camDist)).y;
        float topUIWorldHeight = uiTopY - uiBottomY;

        return new CameraBounds
        {
            minX = bottomLeft.x,
            maxX = topRight.x,
            minY = bottomLeft.y,
            maxY = topRight.y,
            topY = topRight.y - topUIWorldHeight,  // netto topbar
            spawnY = topRight.y + spawnMargin,
        };
    }
}