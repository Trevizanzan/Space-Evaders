using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Prefabs by Size")]
    [SerializeField] private GameObject[] littleAsteroids;
    [SerializeField] private GameObject[] mediumAsteroids;
    [SerializeField] private GameObject[] bigAsteroids;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] topSpawnPoints; // Per spawn normali (verticali)
    [SerializeField] private Transform[] leftSpawnPoints; // Per diagonali/orizzontali da sinistra
    [SerializeField] private Transform[] rightSpawnPoints; // Per diagonali/orizzontali da destra

    [Header("Base Spawn Intervals")]
    [SerializeField] private float baseNormalInterval = 2f;
    [SerializeField] private float baseDiagonalInterval = 3f;
    [SerializeField] private float baseHorizontalInterval = 4f;

    [Header("Base Speeds")]
    [SerializeField] private float baseNormalSpeed = 6f;
    [SerializeField] private float baseDiagonalSpeed = 7.5f;
    [SerializeField] private float baseHorizontalSpeed = 9f;

    [Header("Size Weights (quando asteroidSizeFocus = 0)")]
    [SerializeField] private float smallWeight = 0.5f;
    [SerializeField] private float mediumWeight = 0.3f;
    [SerializeField] private float largeWeight = 0.2f;

    // Timer per i diversi tipi di spawn
    private float normalSpawnTimer;
    private float diagonalSpawnTimer;
    private float horizontalSpawnTimer;

    [Header("Spawn Offsets")]
    [SerializeField] private float horizontalOffset = 1f;
    [SerializeField] private float topOffset = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugAsteroidOnly = false; // disabilita nemici, spawna solo asteroidi
    [SerializeField] private bool debugNormalOnly = false;
    [SerializeField] private bool debugDiagonalOnly = false;
    [SerializeField] private bool debugHorizontalOnly = false;
    [SerializeField] private float debugSpawnInterval = 2f;

    private DifficultyManager difficultyManager;

    //[Header("Spawn Settings")]
    ////[SerializeField] private float spawnWidth = 10f; // Larghezza spawn orizzontale

    // Bordi camera calcolati una volta
    private float minX;
    private float maxX;
    private float spawnY;    // TODO: aumentarlo di un offset per evitare spawn troppo vicini alla camera (grandezza asteroide)
    private float cameraWidth;
    private float cameraHeight;

    // Aggiungi questi campi
    private float leftSpawnX;
    private float rightSpawnX;
    private float sideSpawnMinY;
    private float sideSpawnMaxY;

    public static AsteroidSpawner Instance { get; private set; }
    public static bool IsDebugMode => Instance != null && Instance.debugAsteroidOnly;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        difficultyManager = FindFirstObjectByType<DifficultyManager>();

        // Calcola i bordi della camera UNA VOLTA SOLA
        CalculateCameraBounds();
    }

    void CalculateCameraBounds()
    {
        float camDistance = -Camera.main.transform.position.z;
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));
        Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, camDistance));
        minX = bottomLeft.x + horizontalOffset;
        maxX = topRight.x - horizontalOffset;
        spawnY = topRight.y + topOffset;
        cameraHeight = Camera.main.orthographicSize;
        cameraWidth = cameraHeight * Camera.main.aspect;

        // Calcola i range di spawn laterali basati sulla posizione centrale della camera
        float camCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, camDistance)).y;

        leftSpawnX = bottomLeft.x - 1f;   // appena fuori dal bordo sinistro
        rightSpawnX = -(bottomLeft.x - 1f); // appena fuori dal bordo destro
        sideSpawnMinY = camCenter + 1f;            // dalla metà camera in su
        sideSpawnMaxY = spawnY + 5f;      // fino a 3 unità sopra il bordo superiore
    }

    void Update()
    {
        if (difficultyManager == null) return;

        if (debugAsteroidOnly)
        {
            UpdateDebug();
            return;
        }

        LevelProfile currentLevel = difficultyManager.GetCurrentLevelProfile();
        PhaseConfig currentPhase = currentLevel.GetPhaseConfig(difficultyManager.GetCurrentPhase());

        // Gestione spawn normali (verticali)
        if (currentPhase.spawnNormal)
        {
            HandleNormalSpawn(currentPhase);
        }
        // Gestione spawn diagonali
        if (currentPhase.spawnDiagonal)
        {
            HandleDiagonalSpawn(currentPhase);
        }
        // Gestione spawn orizzontali
        if (currentPhase.spawnHorizontal)
        {
            HandleHorizontalSpawn(currentPhase);
        }
    }

    // ======================== DEBUG ========================

    private float debugTimer;

    void UpdateDebug()
    {
        debugTimer += Time.deltaTime;
        if (debugTimer < debugSpawnInterval) return;
        debugTimer = 0f;

        // Crea una PhaseConfig fittizia con tutti i moltiplicatori a 1
        PhaseConfig debugPhase = new PhaseConfig
        {
            speedMultiplier = 1f,
            healthMultiplier = 1f,
            asteroidSizeFocus = 0,
            normalSpawnMultiplier = 1f,
            diagonalSpawnMultiplier = 1f,
            horizontalSpawnMultiplier = 1f
        };

        if (debugNormalOnly) SpawnNormalAsteroid(debugPhase);
        if (debugDiagonalOnly) SpawnDiagonalAsteroid(debugPhase);
        if (debugHorizontalOnly) SpawnHorizontalAsteroid(debugPhase);
    }

    // ======================== SPAWN NORMALI (VERTICALI) ========================

    void HandleNormalSpawn(PhaseConfig phase)
    {
        normalSpawnTimer += Time.deltaTime;

        float adjustedInterval = baseNormalInterval / phase.normalSpawnMultiplier;
        if (normalSpawnTimer >= adjustedInterval)
        {
            SpawnNormalAsteroid(phase);
            normalSpawnTimer = 0f;
        }
    }

    void SpawnNormalAsteroid(PhaseConfig phase)
    {
        GameObject asteroidPrefab = GetAsteroidBySize(phase.asteroidSizeFocus);
        if (asteroidPrefab == null) return;
        // Usa topSpawnPoints se disponibili, altrimenti calcola posizione casuale
        Vector3 spawnPosition = GetSpawnPosition(topSpawnPoints, minX, maxX, spawnY);
        GameObject asteroid = Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity);
        // Applica velocità verticale (verso il basso)
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.down * baseNormalSpeed * phase.speedMultiplier;
        }
        ApplyHealthMultiplier(asteroid, phase.healthMultiplier);
    }

    // ======================== SPAWN DIAGONALI ========================
    void HandleDiagonalSpawn(PhaseConfig phase)
    {
        diagonalSpawnTimer += Time.deltaTime;
        float adjustedInterval = baseDiagonalInterval / phase.diagonalSpawnMultiplier;
        if (diagonalSpawnTimer >= adjustedInterval)
        {
            SpawnDiagonalAsteroid(phase);
            diagonalSpawnTimer = 0f;
        }
    }
    void SpawnDiagonalAsteroid(PhaseConfig phase)
    {
        GameObject asteroidPrefab = GetAsteroidBySize(phase.asteroidSizeFocus);
        if (asteroidPrefab == null) return;
        // 50% spawn da sinistra, 50% da destra
        bool spawnFromLeft = Random.value > 0.5f;
        Transform[] spawnPoints = spawnFromLeft ? leftSpawnPoints : rightSpawnPoints;

        // (spawn fuori dai bordi laterali)
        float spawnX = spawnFromLeft ? leftSpawnX : rightSpawnX;
        float spawnY = Random.Range(sideSpawnMinY, sideSpawnMaxY);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);
        
        GameObject asteroid = Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity);
        // Direzione diagonale: da sinistra → giù-destra, da destra → giù-sinistra
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 direction = spawnFromLeft ? new Vector2(1f, -1f) : new Vector2(-1f, -1f);
            rb.linearVelocity = direction.normalized * baseDiagonalSpeed * phase.speedMultiplier;
        }
        ApplyHealthMultiplier(asteroid, phase.healthMultiplier);
    }
    
    // ======================== SPAWN ORIZZONTALI ========================
    void HandleHorizontalSpawn(PhaseConfig phase)
    {
        horizontalSpawnTimer += Time.deltaTime;
        float adjustedInterval = baseHorizontalInterval / phase.horizontalSpawnMultiplier;
        if (horizontalSpawnTimer >= adjustedInterval)
        {
            SpawnHorizontalAsteroid(phase);
            horizontalSpawnTimer = 0f;
        }
    }
    void SpawnHorizontalAsteroid(PhaseConfig phase)
    {
        GameObject asteroidPrefab = GetAsteroidBySize(phase.asteroidSizeFocus);
        if (asteroidPrefab == null) return;
        bool spawnFromLeft = Random.value > 0.5f;
        Transform[] spawnPoints = spawnFromLeft ? leftSpawnPoints : rightSpawnPoints;

        // (spawn fuori dai bordi laterali)
        float spawnX = spawnFromLeft ? leftSpawnX : rightSpawnX;
        float spawnY = Random.Range(sideSpawnMinY, sideSpawnMaxY);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

        GameObject asteroid = Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity);
        // Movimento orizzontale puro
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float direction = spawnFromLeft ? 1f : -1f; // 1 = destra, -1 = sinistra
            rb.linearVelocity = Vector2.right * direction * baseHorizontalSpeed * phase.speedMultiplier;
        }
        ApplyHealthMultiplier(asteroid, phase.healthMultiplier);
    }

    // ======================== HELPER METHODS ========================
    
    /// <summary>
    /// Seleziona un asteroide in base al asteroidSizeFocus della fase
    /// </summary>
    GameObject GetAsteroidBySize(int sizeFocus)
    {
        GameObject[] selectedArray = null;
        switch (sizeFocus)
        {
            case 0: // Mix casuale con pesi
                selectedArray = GetRandomSizeArray();
                break;
            case 1: // Solo piccoli
                selectedArray = littleAsteroids;
                break;
            case 2: // Solo medi
                selectedArray = mediumAsteroids;
                break;
            case 3: // Solo grandi
                selectedArray = bigAsteroids;
                break;
        }
        if (selectedArray == null || selectedArray.Length == 0)
        {
            Debug.LogWarning("Nessun asteroide disponibile per sizeFocus: " + sizeFocus);
            return null;
        }
        return selectedArray[Random.Range(0, selectedArray.Length)];
    }

    /// <summary>
    /// Seleziona casualmente un array di asteroidi in base ai pesi configurati
    /// </summary>
    GameObject[] GetRandomSizeArray()
    {
        float totalWeight = smallWeight + mediumWeight + largeWeight;
        float randomValue = Random.Range(0f, totalWeight);

        if (randomValue < smallWeight)
            return littleAsteroids;
        else if (randomValue < smallWeight + mediumWeight)
            return mediumAsteroids;
        else
            return bigAsteroids;
    }

    /// <summary>
    /// Ottiene una posizione di spawn da un array di spawn points o calcola una casuale
    /// </summary>
    Vector3 GetSpawnPosition(Transform[] spawnPoints, float fallbackMinX, float fallbackMaxX, float fallbackY)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform selectedPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return selectedPoint.position;
        }
        else
        {
            // Fallback: calcola posizione casuale con i parametri forniti
            float randomX = Random.Range(fallbackMinX, fallbackMaxX);
            return new Vector3(randomX, fallbackY, 0f);
        }
    }

    /// <summary>
    /// Applica il moltiplicatore di vita all'asteroide (TODO: implementare quando AsteroidHealth esiste)
    /// </summary>
    void ApplyHealthMultiplier(GameObject asteroid, float multiplier)
    {
        // TODO: Implementare quando esiste il sistema di vita
        // Esempio:
        // AsteroidHealth health = asteroid.GetComponent<AsteroidHealth>();
        // if (health != null)
        // {
        //     health.maxHealth *= multiplier;
        //     health.currentHealth = health.maxHealth;
        // }
    }
}