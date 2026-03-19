using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        Debug.Log("Player took damage. Current health: " + currentHealth);
        if (currentHealth <= 0)
        {
            //Destroy(gameObject);
            GameManager.GetInstance().GameOver();
        }
    }
}
