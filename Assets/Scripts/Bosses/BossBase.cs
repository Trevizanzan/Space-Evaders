using UnityEngine;
using System.Collections;

public abstract class BossBase : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] protected int maxHealth = 20;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected GameObject explosionDiePrefab;

    public static bool IsBossEntering { get; private set; } = false;
    protected int currentHealth;
    protected bool isEntering = true;
    protected bool isDead = false;

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        // Mostra la barra della vita
        if (BossHealthBar.Instance != null)
            BossHealthBar.Instance.ShowBar(maxHealth);

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
        Vector3 startPos = new Vector3(0, cameraTop + 1f, 0); // Poco sopra la camera
        Vector3 targetPos = new Vector3(0, cameraTop - 2f, 0); // Dentro la camera

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
        // Override per effetti custom (flash rosso, shake, ecc.)
        // Base: esplosione piccola davanti al boss

        // Spawna esplosione davanti al boss (z negativo = più avanti)
        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);
        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.SpawnSmall(explosionPos, 1f);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Nascondi la barra
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.HideBar();
        }

        // Posizione davanti al boss
        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);

        // Esplosione
        if (explosionDiePrefab != null && ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.SpawnBig(explosionPos, 2.5f);
            ExplosionManager.Instance.SpawnBig(explosionPos + Vector3.right * 1f, 1f);
            ExplosionManager.Instance.SpawnBig(explosionPos + Vector3.left * 1f, 1f);
        }
        else
        {
            Debug.LogWarning("ExplosionManager.Instance is null!");
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