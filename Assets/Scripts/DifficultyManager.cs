using System.Collections;
using TMPro;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Wave Settings")]
    [SerializeField] private float waveDuration = 60f; // durata wave in secondi (divisa in 3 fasi)
    [SerializeField] private float transitionDuration = 3f; // pausa tra wave e boss

    [Header("Difficulty Scaling")]
    [SerializeField] private float globalDifficultyMultiplier = 1f;
    [SerializeField] private float difficultyIncreasePerLoop = 0.5f; // +50% ogni loop completo

    [Header("Boss System")]
    [SerializeField] private GameObject[] bossPrefabs; // 6 boss in ordine
    private int bossIndex = 0;
    private int totalBossesDefeated = 0; // Traccia quanti boss hai battuto in totale
    private bool isBossFight = false;

    [Header("Debug")]
    [SerializeField] private bool skipToFirstBoss = false;
    private int debugBossIndex = 0;

    // Wave state
    private float waveTime = 0f;
    private float progress = 0f; // 0–1
    private bool isInTransition = false;
    // Events
    public System.Action OnWaveComplete;
    public GameObject levelCompletePanel;
    public TMP_Text levelCompleteCountdown;


     //OLD
    //[Header("Level Settings")]
    //[SerializeField] private float levelDuration = 60f; // durata livello in secondi
    // TODO: endless mode non serve a nulla 
    //[SerializeField] private bool endlessMode = false; // se true, continua a scalare oltre levelDuration
    //[SerializeField] private int maxLevels = 3; // quanti livelli prima di endless

    [Header("Difficulty Curves (0 = inizio, 1 = fine livello)")]
    [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Linear(0, 1.5f, 1, 0.4f);
    [SerializeField] private AnimationCurve fallSpeedCurve = AnimationCurve.Linear(0, 4f, 1, 8f);
    [SerializeField] private AnimationCurve asteroidHealthMultiplier = AnimationCurve.Constant(0, 1, 1f);

    private int currentLevel = 1;
    private float levelTime = 0f;
    public System.Action OnLevelComplete; // evento per notificare altri sistemi (es. UI) quando un livello è completato


    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnWaveComplete += ShowWaveComplete;
        }

        // Debug: salta direttamente al primo boss
        if (skipToFirstBoss)
        {
            StartCoroutine(DebugSkipToBoss());
        }
    }

    void ShowWaveComplete()
    {
        StartCoroutine(WaveCompleteRoutine());
    }
    IEnumerator WaveCompleteRoutine()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            if (levelCompleteCountdown != null)
                levelCompleteCountdown.text = $"WAVE COMPLETE! \n Boss incoming in {i}...";
            yield return new WaitForSeconds(1f);
        }
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    //void ShowLevelComplete()
    //{
    //    StartCoroutine(LevelCompleteRoutine());
    //}
    //// Mostra pannello di completamento livello e countdown, poi nasconde
    //IEnumerator LevelCompleteRoutine()
    //{
    //    if (levelCompletePanel != null)
    //        levelCompletePanel.SetActive(true);

    //    for (int i = 3; i > 0; i--)
    //    {
    //        if (levelCompleteCountdown != null)
    //            levelCompleteCountdown.text = $"LEVEL COMPLETE! \n Next level in {i}...";

    //        yield return new WaitForSeconds(1f);
    //    }
    //    if (levelCompletePanel != null)
    //        levelCompletePanel.SetActive(false);
    //}

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (isInTransition) return;
        if (isBossFight) return;

        waveTime += Time.deltaTime;
        progress = Mathf.Clamp01(waveTime / waveDuration);  // clamp è importante per evitare valori strani dopo waveDuration

        //// Check se livello finito
        //if (levelTime >= levelDuration)
        //{
        //    // Decidi se è tempo di boss o transizione normale
        //    if (currentLevel == 3) // dopo livello 3, spawna boss
        //    {
        //        StartCoroutine(BossTransition());
        //    }
        //    else
        //    {
        //        StartCoroutine(LevelTransition()); // transizione normale
        //    }
        //}

        // Check se wave finita → spawna boss
        if (waveTime >= waveDuration)
        {
            StartCoroutine(BossTransition());
        }
    }

    //// Gestisce la transizione tra livelli: notifica, aspetta, resetta timer e progress, incrementa livello
    //IEnumerator LevelTransition()
    //{
    //    isInTransition = true;
        
    //    // Notifica che il livello è completato
    //    OnLevelComplete?.Invoke();
    //    yield return new WaitForSeconds(transitionDuration);

    //    // Avanza livello
    //    currentLevel++;
    //    levelTime = 0f;
    //    progress = 0f;
    //    isInTransition = false;
    //}

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUILayout.Label($"Wave Time: {waveTime:F1}s");
        GUILayout.Label($"Progress: {progress:F2}");
        GUILayout.Label($"Phase: {GetCurrentPhase()}");
        GUILayout.Label($"Bosses Defeated: {totalBossesDefeated}");
        GUILayout.Label($"Global Multiplier: {globalDifficultyMultiplier:F2}x");
        GUILayout.Label($"Boss Fight: {isBossFight}");
    }

    #region Boss System

    // Gestisce la transizione al boss: notifica, ferma spawn asteroidi, aspetta, spawna boss, attiva modalità boss fight
    IEnumerator BossTransition()
    {
        isInTransition = true;
        OnLevelComplete?.Invoke();

        // Ferma spawn asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = false;

        // TODO: Ferma anche EnemySpawner quando lo crei
        // EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        // if (enemySpawner != null) enemySpawner.enabled = false;

        yield return new WaitForSeconds(transitionDuration);

        // Spawna il boss corrente con posizione e rotazione corrette
        if (bossIndex < bossPrefabs.Length && bossPrefabs[bossIndex] != null)
        {
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop + 1f, 0);
            Instantiate(bossPrefabs[bossIndex], spawnPos, bossPrefabs[bossIndex].transform.rotation);
        }

        // Attiva il boss fight mode
        isBossFight = true;
        isInTransition = false;
    }

    public void OnBossDefeated()
    {
        // Disattiva boss fight mode
        isBossFight = false;
        bossIndex++;
        totalBossesDefeated++; // Incrementa il contatore totale

        // TODO: game finale??
        // Se hai battuto tutti e 6 i boss, ricomincia loop con difficoltà aumentata
        if (bossIndex >= bossPrefabs.Length)
        {
            bossIndex = 0;
            globalDifficultyMultiplier += difficultyIncreasePerLoop; // +50% difficoltà ogni loop
        }

        // Resetta wave
        waveTime = 0f;
        progress = 0f;

        // Riattiva spawner asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = true;

        // TODO: Riattiva EnemySpawner
        // EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        // if (enemySpawner != null) enemySpawner.enabled = true;

        StartCoroutine(WaveStartDelay()); // normale transizione
    }

    IEnumerator WaveStartDelay()
    {
        isInTransition = true;
        yield return new WaitForSeconds(2f); // Breve pausa prima di ricominciare
        isInTransition = false;
    }

    // Metodo debug per testare il boss direttamente come primo livello
    IEnumerator DebugSkipToBoss()
    {
        debugBossIndex = 0; // o qualsiasi boss tu voglia testare

        // Aspetta 1 frame per far inizializzare tutto
        yield return null;

        // Disabilita lo spawner degli asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = false;

        // Disabilita UI testo (livello e timer)
        ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            // Se ScoreManager ha riferimenti pubblici a questi testi
            scoreManager.DisableLevelAndTimerText();
        }

        // Spawna il primo boss (o quello che vuoi testare) con posizione e rotazione corretta
        if (bossPrefabs.Length > 0 && bossPrefabs[debugBossIndex] != null)
        {
            Debug.Log($"[DEBUG] Spawning boss {debugBossIndex} for testing");
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop + 1f, 0);
            Instantiate(bossPrefabs[bossIndex], spawnPos, bossPrefabs[bossIndex].transform.rotation);   // mantengo rotazione del prefab
        }
        else
            Debug.LogWarning("[DEBUG] No boss prefab assigned!");

        // Attiva il boss fight mode
        isBossFight = true;

        // Opzionale: nascondi il pannello di level complete se è attivo
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        else
            Debug.LogWarning("[DEBUG] No level complete panel assigned!");
    }

    #endregion

    #region Public Getters for Spawners

    /// <summary>
    /// Ritorna la fase corrente (1, 2 o 3) in base al progress della wave
    /// </summary>
    public int GetCurrentPhase()
    {
        // FOR DEBUG:
        //return 3;

        if (progress < 0.33f) return 1; // Fase 1: 0-20s
        if (progress < 0.66f) return 2; // Fase 2: 20-40s
        return 3;                       // Fase 3: 40-60s
    }

    /// <summary>
    /// Controlla se un certo tipo di nemico può spawnare in base a quanti boss sono stati sconfitti
    /// </summary>
    public bool CanSpawnEnemyType(string enemyType)
    {
        switch (enemyType)
        {
            case "Fighter":
                return totalBossesDefeated >= 0; // Appare da subito (dopo boss 0 = primo ciclo)
            case "Kamikaze":
                return totalBossesDefeated >= 3; // Appare solo dopo il boss 3
            case "Bomber":
                return totalBossesDefeated >= 6; // Appare solo dopo il boss 6 (secondo loop)
            default:
                return true; // Tipi sconosciuti sempre attivi
        }
    }

    /// <summary>
    /// Ritorna true se tutte le fasi dovrebbero essere attive contemporaneamente (chaos mode)
    /// </summary>
    public bool IsAllPhasesActive()
    {
        // Dopo il boss 6 (secondo loop), tutte le fasi sono sempre attive
        return totalBossesDefeated >= 6;
    }

    public float GetSpawnRate()
    {
        if (isInTransition) return 999f;

        //// Calcola baseline per questo livello (parte più alta)
        //float levelBaseline = Mathf.Max(0.4f, 1.5f - (currentLevel - 1) * 0.3f); // livello 1: 1.5, livello 2: 1.2, livello 3: 0.9
        //float levelEnd = Mathf.Max(0.2f, 0.4f - (currentLevel - 1) * 0.1f);      // livello 1: 0.4, livello 2: 0.3, livello 3: 0.2

        //// Interpola dalla baseline all'end in base al progress del livello
        ////return Mathf.Lerp(levelBaseline, levelEnd, progress);

        //// baseRate è il tempo tra spawn all'inizio del livello, diminuisce fino a levelEnd alla fine del livello
        //// nuymero più alto = spawn meno frequente = più facile
        //// numero più basso = spawn più frequente = più difficile
        //float baseRate = Mathf.Lerp(levelBaseline, levelEnd, progress);
        //// Applica il moltiplicatore globale (diminuisce il tempo = più difficile)
        //return baseRate / globalDifficultyMultiplier;

        // Semplificato: un singolo calcolo scalato dal progress
        float baseRate = Mathf.Lerp(1.5f, 0.4f, progress); // Da 1.5s a 0.4s durante la wave
        return baseRate / globalDifficultyMultiplier;
    }

    public float GetFallSpeed()
    {
        //float levelBaseline = 4f + (currentLevel - 1) * 2f;  // livello 1: 4, livello 2: 6, livello 3: 8
        //float levelEnd = 8f + (currentLevel - 1) * 2f;       // livello 1: 8, livello 2: 10, livello 3: 12

        ////return Mathf.Lerp(levelBaseline, levelEnd, progress);

        //float baseSpeed = Mathf.Lerp(levelBaseline, levelEnd, progress);

        //// Applica il moltiplicatore globale (aumenta velocità)
        //return baseSpeed * globalDifficultyMultiplier;

        // Semplificato: velocità cresce col progress
        float baseSpeed = Mathf.Lerp(4f, 8f, progress); // Da 4 a 8 durante la wave
        return baseSpeed * globalDifficultyMultiplier;
    }

    public float GetProgress() => progress;
    public float GetWaveTime() => waveTime;
    public float GetTimeRemaining() => Mathf.Max(0, waveDuration - waveTime);
    public bool IsInTransition() => isInTransition;
    public int GetTotalBossesDefeated() => totalBossesDefeated;
    public float GetGlobalMultiplier() => globalDifficultyMultiplier;

    public float GetHealthMultiplier() => asteroidHealthMultiplier.Evaluate(progress);
    public float GetLevelTime() => levelTime;
    public int GetCurrentLevel() => currentLevel;

    #endregion
}