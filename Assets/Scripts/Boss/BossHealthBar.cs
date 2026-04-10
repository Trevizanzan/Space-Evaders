using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f;

    private int currentHealth;
    private int maxHealth;
    private float currentFillAmount;
    private float targetFillAmount;
    private bool isActive = false; // flag interno, indipendente da IsBossFightActive()

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (!isActive) return;

        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        DifficultyManager.Instance?.UpdateBossHealthDirect(currentFillAmount);

        // Quando il lerp è abbastanza vicino allo 0, fermati
        if (targetFillAmount <= 0f && currentFillAmount < 0.01f)
        {
            currentFillAmount = 0f;
            DifficultyManager.Instance?.UpdateBossHealthDirect(0f);
            isActive = false;
        }
    }

    public void ShowBar(int maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
        targetFillAmount = 1f;
        currentFillAmount = 1f;
        isActive = true;

        DifficultyManager.Instance?.ShowBossUI();
    }

    public void UpdateHealth(int currentHP)
    {
        currentHealth = currentHP;
        targetFillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
    }

    public void SetBossName(string name)
    {
        DifficultyManager.Instance?.SetBossName(name);
    }
}