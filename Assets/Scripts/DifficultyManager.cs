using System.Collections;
using TMPro;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Level Settings")]
    [SerializeField] private float levelDuration = 60f; // durata livello in secondi
    // TODO: endless mode non serve a nulla 
    //[SerializeField] private bool endlessMode = false; // se true, continua a scalare oltre levelDuration
    [SerializeField] private int maxLevels = 3; // quanti livelli prima di endless
    [SerializeField] private float transitionDuration = 3f; // pausa tra livelli

    [Header("Difficulty Curves (0 = inizio, 1 = fine livello)")]
    [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Linear(0, 1.5f, 1, 0.4f);
    [SerializeField] private AnimationCurve fallSpeedCurve = AnimationCurve.Linear(0, 4f, 1, 8f);
    [SerializeField] private AnimationCurve asteroidHealthMultiplier = AnimationCurve.Constant(0, 1, 1f);

    [Header("Boss System")]
    [SerializeField] private GameObject[] bossPrefabs; // 6 boss in ordine
    private int bossIndex = 0;
    private int debugBossIndex = 0;
    private bool isBossFight = false;

    [Header("Debug")]
    [SerializeField] private bool skipToFirstBoss = false; // Attiva nell'Inspector per testare

    private int currentLevel = 1;
    private float levelTime = 0f;
    private float progress = 0f; // 0–1
    private bool isInTransition = false;
    private float globalDifficultyMultiplier = 1f; 

    public System.Action OnLevelComplete; // evento per notificare altri sistemi (es. UI) quando un livello è completato

    public GameObject levelCompletePanel;
    public TMP_Text levelCompleteCountdown;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnLevelComplete += ShowLevelComplete;
        }

        // Debug: salta direttamente al primo boss
        if (skipToFirstBoss)
        {
            StartCoroutine(DebugSkipToBoss());
        }
    }
    void ShowLevelComplete()
    {
        StartCoroutine(LevelCompleteRoutine());
    }
    // Mostra pannello di completamento livello e countdown, poi nasconde
    IEnumerator LevelCompleteRoutine()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            if (levelCompleteCountdown != null)
                levelCompleteCountdown.text = $"LEVEL COMPLETE! \n Next level in {i}...";

            yield return new WaitForSeconds(1f);
        }
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (isInTransition) return;

        if (isBossFight) return;

        levelTime += Time.deltaTime;
        //progress = endlessMode
        //    ? Mathf.Clamp01(levelTime / (levelDuration * 2f)) // scala più lento in endless
        //    : Mathf.Clamp01(levelTime / levelDuration);
        progress = Mathf.Clamp01(levelTime / levelDuration);

        // Check se livello finito
        if (levelTime >= levelDuration)
        {
            // Decidi se è tempo di boss o transizione normale
            if (currentLevel == 3) // dopo livello 3, spawna boss
            {
                StartCoroutine(BossTransition());
            }
            else
            {
                StartCoroutine(LevelTransition()); // transizione normale
            }
        }
    }

    // Gestisce la transizione tra livelli: notifica, aspetta, resetta timer e progress, incrementa livello
    IEnumerator LevelTransition()
    {
        isInTransition = true;
        
        // Notifica che il livello è completato
        OnLevelComplete?.Invoke();
        yield return new WaitForSeconds(transitionDuration);

        // Avanza livello
        currentLevel++;
        levelTime = 0f;
        progress = 0f;
        isInTransition = false;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.Label($"Level Time: {levelTime:F1}s");
        GUILayout.Label($"Progress: {progress:F2}");
        GUILayout.Label($"Spawn Rate: {GetSpawnRate():F2}s");
        GUILayout.Label($"Fall Speed: {GetFallSpeed():F1}");
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

        currentLevel++;
        isInTransition = false;
    }

    public void OnBossDefeated()
    {
        // Disattiva boss fight mode
        isBossFight = false;

        bossIndex++;
        
        // TODO: game finale??
        // Se hai battuto tutti e 6 i boss, ricomincia loop con difficoltà aumentata
        if (bossIndex >= bossPrefabs.Length)
        {
            bossIndex = 0;
            globalDifficultyMultiplier += 0.5f; // +50% difficoltà ogni loop
        }

        // Riavvia gioco dal livello 1
        currentLevel = 1;
        levelTime = 0f;
        progress = 0f;

        // Riattiva asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = true;

        StartCoroutine(LevelTransition()); // normale transizione
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

        // Spawna il primo boss (o quello che vuoi testare) con posizione e rotazione corretta
        if (bossPrefabs.Length > 0 && bossPrefabs[debugBossIndex] != null)
        {
            Debug.Log($"[DEBUG] Spawning boss {debugBossIndex} for testing");

            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop + 1f, 0);
            Instantiate(bossPrefabs[bossIndex], spawnPos, bossPrefabs[bossIndex].transform.rotation);   // mantengo rotazione del prefab
        }
        else
        {
            Debug.LogWarning("[DEBUG] No boss prefab assigned!");
        }

        // Attiva il boss fight mode
        isBossFight = true;
        currentLevel = 4; // Simula di essere dopo il livello 3

        // Opzionale: nascondi il pannello di level complete se è attivo
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    #endregion

    //public float GetSpawnRate() => spawnRateCurve.Evaluate(progress);
    //public float GetFallSpeed() => fallSpeedCurve.Evaluate(progress);
    public float GetSpawnRate()
    {
        if (isInTransition) return 999f;

        // Calcola baseline per questo livello (parte più alta)
        float levelBaseline = Mathf.Max(0.4f, 1.5f - (currentLevel - 1) * 0.3f); // livello 1: 1.5, livello 2: 1.2, livello 3: 0.9
        float levelEnd = Mathf.Max(0.2f, 0.4f - (currentLevel - 1) * 0.1f);      // livello 1: 0.4, livello 2: 0.3, livello 3: 0.2

        // Interpola dalla baseline all'end in base al progress del livello
        //return Mathf.Lerp(levelBaseline, levelEnd, progress);

        // baseRate è il tempo tra spawn all'inizio del livello, diminuisce fino a levelEnd alla fine del livello
        // nuymero più alto = spawn meno frequente = più facile
        // numero più basso = spawn più frequente = più difficile
        float baseRate = Mathf.Lerp(levelBaseline, levelEnd, progress);
        // Applica il moltiplicatore globale (diminuisce il tempo = più difficile)
        return baseRate / globalDifficultyMultiplier;
    }

    public float GetFallSpeed()
    {
        float levelBaseline = 4f + (currentLevel - 1) * 2f;  // livello 1: 4, livello 2: 6, livello 3: 8
        float levelEnd = 8f + (currentLevel - 1) * 2f;       // livello 1: 8, livello 2: 10, livello 3: 12

        //return Mathf.Lerp(levelBaseline, levelEnd, progress);
        
        float baseSpeed = Mathf.Lerp(levelBaseline, levelEnd, progress);

        // Applica il moltiplicatore globale (aumenta velocità)
        return baseSpeed * globalDifficultyMultiplier;
    }

    public float GetHealthMultiplier() => asteroidHealthMultiplier.Evaluate(progress);
    public float GetProgress() => progress;
    public float GetLevelTime() => levelTime;
    public int GetCurrentLevel() => currentLevel;
    public float GetTimeRemaining() => Mathf.Max(0, levelDuration - levelTime);
    public bool IsInTransition() => isInTransition;
}