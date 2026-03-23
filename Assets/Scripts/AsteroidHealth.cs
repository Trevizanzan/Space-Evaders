using UnityEngine;

public class AsteroidHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    [SerializeField] private int scoreValue = 10;  // punti per questo tipo di asteroide

    void Awake()
    {
        currentHealth = maxHealth;
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