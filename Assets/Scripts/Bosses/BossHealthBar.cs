using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance;

    [Header("UI References")]
    [SerializeField] private GameObject healthBarContainer;
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f; // Velocità transizione smooth

    private int currentHealth;
    private int maxHealth;
    private float targetFillAmount;

    void Awake()
    {
        Debug.Log("[HEALTH_BAR] Awake called");

        // Singleton
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[HEALTH_BAR] Instance created!");
        }
        else
            Destroy(gameObject);

        // Nascondi all'inizio
        if (healthBarContainer != null)
            healthBarContainer.SetActive(false);
        else
            Debug.LogError("[HEALTH_BAR] healthBarContainer NOT ASSIGNED!");
    }

    void Update()
    {
        // Smooth transition della barra
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        }
    }

    /// <summary>
    /// Mostra la barra e inizializza con la vita massima del boss
    /// </summary>
    public void ShowBar(int maxHP)
    {
        Debug.Log($"[HEALTH_BAR] ShowBar called with maxHP={maxHP}");

        maxHealth = maxHP;
        currentHealth = maxHP;
        targetFillAmount = 1f;

        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
            Debug.Log($"[HEALTH_BAR] fillImage.fillAmount set to 1.0");
        }
        else
            Debug.LogError("[HEALTH_BAR] fillImage NOT ASSIGNED!");

        if (healthBarContainer != null)
            healthBarContainer.SetActive(true);
        else
            Debug.LogError("[HEALTH_BAR] healthBarContainer is null!");
    }

    /// <summary>
    /// Aggiorna la barra quando il boss prende danno
    /// </summary>
    public void UpdateHealth(int currentHP)
    {
        Debug.Log($"[HEALTH_BAR] UpdateHealth called: {currentHP}/{maxHealth}");

        currentHealth = currentHP;
        targetFillAmount = (float)currentHealth / maxHealth;

        // Clamp tra 0 e 1
        targetFillAmount = Mathf.Clamp01(targetFillAmount);

        Debug.Log($"[HEALTH_BAR] targetFillAmount set to {targetFillAmount}");
    }

    /// <summary>
    /// Nascondi la barra quando il boss muore
    /// </summary>
    public void HideBar()
    {
        Debug.Log("[HEALTH_BAR] HideBar called");

        if (healthBarContainer != null)
            healthBarContainer.SetActive(false);
    }
}