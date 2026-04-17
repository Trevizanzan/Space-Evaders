using System.Collections;
using UnityEngine;

public class AsteroidHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    [SerializeField] private int scoreValue = 10;  // punti per questo tipo di asteroide

    [Header("Hit Feedback")]
    [SerializeField] private Material flashMaterial;  
    [SerializeField] private float flashDuration = 0.1f;  
    private SpriteRenderer sr;           
    private Material originalMaterial;
    private Coroutine flashCoroutine;

    void Awake()
    {
        currentHealth = maxHealth;

        sr = GetComponent<SpriteRenderer>();                  
        if (sr != null) originalMaterial = sr.material;      

        //// Opzionale: scalare la salute in base alla difficoltà attuale
        //float multiplier = DifficultyManager.Instance != null
        //    ? DifficultyManager.Instance.GetHealthMultiplier()
        //    : 1f;
        //currentHealth = Mathf.RoundToInt(maxHealth * multiplier);
    }

    // Applica danno all'asteroide, se muore, aggiungi punteggio
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        FlashWhite();  // sempre feedback visivo, sia hit che kill

        if(currentHealth <= 0)
        { 
            Die();
        }
    }

    private void Die()
    {
        if (RunStats.Instance != null)
            RunStats.Instance.RegisterAsteroidDestroyed();

        // suona l'effetto sonoro di esplosione
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayAsteroidExplode();
        }

        // Fai esplodere l'asteroide (animazione)
        if (ExplosionManager.Instance != null)
        {
            // Se vuoi distinguere piccoli/grandi:
            // TODO: aggiungere un parametro "size" o "type" per decidere quale esplosione spawnare
            // TODO: aggiungere tipo un boolean "isLarge" o un enum "AsteroidType { Small, Medium, Large }"
            if (maxHealth >= 3) ExplosionManager.Instance.SpawnBig(transform.position, 1.8f);
            else ExplosionManager.Instance.SpawnSmall(transform.position, 0.8f);
        }

        // Aggiungi punti allo score UI
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        // distruggi l'asteroide
        Destroy(gameObject, 0.08f);
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
}