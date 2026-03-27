using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // suona effetto colpo
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlayerHit();
        }

        // mostra esplosione grande (animazione)
        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.SpawnBig(transform.position, 2.0f);
        }

        // aggiorna UI vite
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateLivesUI();
        }

        // Controlla se il giocatore è morto
        if (currentHealth <= 0)
        {
            //Destroy(gameObject);

            // disabilita il player (invece di distruggerlo) per mostrare esplosione e suono

            // Disabilita TUTTI i SpriteRenderer (inclusi i child come il motore)
            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in allSprites)
            {
                sr.enabled = false;
            }

            GetComponent<Collider2D>().enabled = false;

            GameManager.GetInstance().GameOver();
        }
    }
}
