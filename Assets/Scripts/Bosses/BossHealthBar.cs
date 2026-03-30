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
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);

        // Nascondi all'inizio
        if (healthBarContainer != null)
            healthBarContainer.SetActive(false);
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
        maxHealth = maxHP;
        currentHealth = maxHP;
        targetFillAmount = 1f;

        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
        }

        if (healthBarContainer != null)
            healthBarContainer.SetActive(true);
    }

    /// <summary>
    /// Aggiorna la barra quando il boss prende danno
    /// </summary>
    public void UpdateHealth(int currentHP)
    {
        currentHealth = currentHP;
        targetFillAmount = (float)currentHealth / maxHealth;

        // Clamp tra 0 e 1
        targetFillAmount = Mathf.Clamp01(targetFillAmount);
    }

    /// <summary>
    /// Nascondi la barra quando il boss muore
    /// </summary>
    public void HideBar()
    {
        if (healthBarContainer != null)
            healthBarContainer.SetActive(false);
    }
}