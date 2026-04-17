using UnityEngine;
using System.Collections;

public abstract class BossBase : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] protected int maxHealth = 20;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected GameObject explosionDiePrefab;

    [Header("Boss Info")]
    [SerializeField] protected string bossDisplayName = "Unknown Boss";

    public static bool IsBossEntering { get; private set; } = false;
    protected int currentHealth;
    protected bool isEntering = true;
    protected bool isDead = false;

    [Header("Hit Flash")]
    [SerializeField] private Material flashMaterial;   // assegna dal Inspector
    private float flashDuration = 0.04f;

    private Material originalMaterial;
    private SpriteRenderer sr;
    private Coroutine flashCoroutine;

    [Header("UI Padding (sincronizzato con Spaceship)")]
    [SerializeField][Range(0f, 0.2f)] protected float topUIPaddingViewport = 0.08f;
    // Proprietà read-only usabile dai figli
    protected float TopUIWorldHeight
    {
        get
        {
            Camera cam = Camera.main;
            float top = cam.ViewportToWorldPoint(new Vector3(0f, 1f, 0f)).y;
            float bottom = cam.ViewportToWorldPoint(new Vector3(0f, 1f - topUIPaddingViewport, 0f)).y;
            return top - bottom;
        }
    }

    protected virtual void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalMaterial = sr.material;

        currentHealth = maxHealth;

        // Mostra la barra della vita
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.ShowBar(maxHealth);
            BossHealthBar.Instance.SetBossName(bossDisplayName);
        }
        else
        {
            Debug.LogError("[BossBase] BossHealthBar.Instance è NULL! Non trovato nella scena!");
        }

        StartCoroutine(EntranceRoutine());

        // Suono di spawn del boss
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBossEntrance(4f);
    }

    /// <summary>
    /// Base: il boss entra dall'alto, fermandosi a una certa posizione centrale.
    /// Durante l'entrata, il player non può sparare.
    /// </summary>
    protected virtual IEnumerator EntranceRoutine()
    {
        // Disabilita sparo del player durante l'entrata
        IsBossEntering = true;

        // Calcola posizioni dinamicamente in base alla camera
        float cameraTop = Camera.main.orthographicSize;

        Vector3 startPos = new Vector3(0, cameraTop * 1.15f, 0); // Poco sopra la camera
        Vector3 targetPos = new Vector3(0, cameraTop * 0.85f, 0); // Dentro la camera
        //Debug.Log($"cameraTop={cameraTop}");
        //Debug.Log($"targetPos={targetPos}");

        transform.position = startPos;

        // Movimento verso il basso fino alla posizione target
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        isEntering = false;

        // Riabilita sparo del player dopo l'entrata
        IsBossEntering = false;

        OnEntranceComplete();
    }

    // Chiamato quando l'entrata è finita, ogni boss inizia il suo pattern qui
    protected virtual void OnEntranceComplete()
    {
        // Override nei boss specifici
    }

    protected virtual void Update()
    {
        if (isDead || isEntering) return;

        // Ogni boss sovrascrive per implementare movimento/attacco
        UpdateBehavior();
    }

    // METODO ASTRATTO: ogni boss DEVE implementarlo
    protected abstract void UpdateBehavior();

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        // Applica danno
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);    // Evita valori negativi

        // Aggiorna la barra della vita
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.UpdateHealth(currentHealth);
        }

        // Suono quando il boss viene colpito
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBossHit();

        // Feedback visivo (flash, shake, ecc.)
        OnDamageFeedback();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void OnDamageFeedback()
    {
        // Base: esplosione piccola davanti al boss

        // Spawna esplosione davanti al boss (z negativo = più avanti)
        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);
        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.SpawnSmall(explosionPos, 1f);
        }
    }

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

        // Posizione davanti al boss
        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);

        // Esplosione
        if (explosionDiePrefab != null && ExplosionManager.Instance != null)
        {
            float offset = Camera.main.orthographicSize * 0.06f;

            ExplosionManager.Instance.SpawnBig(explosionPos, 1.3f);
            //ExplosionManager.Instance.SpawnBig(explosionPos + Vector3.right * offset, 1f);
            //ExplosionManager.Instance.SpawnBig(explosionPos + Vector3.left * offset, 1f);
        }

        // Suono di morte
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBossDead();
        
        // Notifica DifficultyManager
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.OnBossDefeated();    // TODO: suono personalizzato

        // Distruggi il boss dopo un breve delay per permettere all'esplosione di essere visibile
        Destroy(gameObject, 0.266f);
    }

    // Collisione con proiettile player
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("Bullet")) // tag del bullet del player
        {
            TakeDamage(1);
            Destroy(collision.gameObject);
        }
    }
}