using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance;


    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f; // Velocità transizione smooth

    private int currentHealth;
    private int maxHealth;
    private float currentFillAmount; // Attuale fill amount (per smooth lerp)
    private float targetFillAmount; // Target fill amount


    void Awake()
    {
        // Singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // Smooth transition della barra
        if (DifficultyManager.Instance != null && DifficultyManager.Instance.IsBossFightActive())
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);

            // Aggiorna il DifficultyManager con il valore smooth
            DifficultyManager.Instance.UpdateBossHealthDirect(currentFillAmount);
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
        currentFillAmount = 1f;
        
        // Notifica al DifficultyManager (gestisce la UI nella TopBar)
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ShowBossUI();
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

        // L'aggiornamento smooth avviene in Update() tramite Lerp

    }

    /// <summary>
    /// Imposta il nome del boss
    /// </summary>
    public void SetBossName(string name)
    {
        // Passa il nome al DifficultyManager
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.SetBossName(name);
    }
}