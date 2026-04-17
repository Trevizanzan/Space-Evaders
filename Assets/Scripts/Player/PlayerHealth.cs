using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;
    public int CurrentHealth => currentHealth;

    [Header("I-Frames")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private float blinkInterval = 0.1f;  // ogni quanto appare/sparisce
    private bool isInvulnerable = false;

    private SpriteRenderer sr;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (RunStats.Instance != null)
            RunStats.Instance.RegisterHitReceived(amount);

        if (isInvulnerable) return;  // blocca danni durante i-frames

        currentHealth -= amount;

        // suona effetto colpo
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerHit();

        // mostra esplosione grande (animazione)
        if (ExplosionManager.Instance != null)
            ExplosionManager.Instance.SpawnBig(transform.position, 2.0f);

        // aggiorna UI vite
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.UpdateLivesUI();

        // Controlla se il giocatore è morto
        if (currentHealth <= 0)
        {
            // disabilita il player (invece di distruggerlo) per mostrare esplosione e suono
            // Disabilita TUTTI i SpriteRenderer (inclusi i child come il motore)
            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
            foreach (var s in allSprites)
                s.enabled = false;

            GetComponent<Collider2D>().enabled = false;
            GameManager.GetInstance().GameOver();
            return; // non avviare i-frames se è morto
        }

        // Avvia i-frames solo se è ancora vivo
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(InvulnerabilityRoutine());
    }

    /// <summary>
    /// I-frames (invincibility frames) — finestra di tempo dopo aver ricevuto un colpo durante la quale il giocatore è invulnerabile. 
    /// Evita che danni multipli ravvicinati (es. asteroide + proiettile nello stesso frame, o attraversamento di un campo di detriti) svuotino la vita in modo frustrante. 
    /// Il blink visivo comunica al giocatore che l'invulnerabilità è attiva.
    /// </summary>
    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;

        float elapsed = 0f;
        while (elapsed < invulnerabilityDuration)
        {
            // Ottieni tutti i SpriteRenderer inclusi i child (es. motore)
            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
            bool visible = !allSprites[0].enabled; // toggle basato sullo stato attuale
            foreach (var s in allSprites)
                s.enabled = visible;

            elapsed += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }

        // Assicurati che il player sia visibile alla fine
        SpriteRenderer[] finalSprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (var s in finalSprites)
            s.enabled = true;

        isInvulnerable = false;
        blinkCoroutine = null;
    }
}
