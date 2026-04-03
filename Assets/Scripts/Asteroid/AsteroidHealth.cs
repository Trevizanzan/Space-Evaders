using UnityEngine;

public class AsteroidHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    [SerializeField] private int scoreValue = 10;  // punti per questo tipo di asteroide

    void Awake()
    {
        currentHealth = maxHealth;

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

        if (currentHealth <= 0)
        {
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
            Destroy(gameObject);
        }
    }
}