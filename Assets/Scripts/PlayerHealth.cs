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

        // aggiorna UI vite
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateLivesUI();
        }

        // Controlla se il giocatore è morto
        if (currentHealth <= 0)
        {
            //Destroy(gameObject);
            GameManager.GetInstance().GameOver();
        }
    }
}
