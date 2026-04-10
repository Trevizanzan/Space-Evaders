using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Base Spawn Intervals")]
    [SerializeField] private float baseFighterInterval = 8f;
    [SerializeField] private float baseKamikazeInterval = 6f;
    [SerializeField] private float baseBomberInterval = 12f;
    [SerializeField] private float basePulsarInterval = 14f;  // leggermente più raro del Bomber

    [Header("Spawn Range Settings")]
    [SerializeField] private float topSpawnMargin = .5f;  // quanto sopra la camera
    [SerializeField] private float sideSpawnMargin = 20.1f;  // quanto fuori dai lati
    [SerializeField] private float topHorizontalInset = 1f;    // rientra dai bordi sinistro/destro (top spawn)
    //[SerializeField] private float sideVerticalInset = 1f;    // rientra dal bordo superiore (side spawn)
    [SerializeField] private float minSpawnDistance = 2f;    // distanza minima tra spawn consecutivi (stessa fascia)
    [SerializeField] private int maxRetries = 10;    // tentativi massimi per trovare posizione valida

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject[] fighterPrefabs;
    [SerializeField] private GameObject[] kamikazePrefabs;
    [SerializeField] private GameObject[] bomberPrefabs;
    [SerializeField] private GameObject[] pulsarPrefabs;

    [Header("Debug - Enemy Test")]
    [SerializeField] private bool debugEnemyOnly = false;
    [SerializeField] private bool debugFighterOnly = false;
    [SerializeField] private bool debugKamikazeOnly = false;
    [SerializeField] private bool debugBomberOnly = false;
    [SerializeField] private bool debugPulsarOnly = false;
    [SerializeField] private float debugSpawnInterval = 3f;
    // Proprietà statica leggibile da altri sistemi
    public static bool IsDebugMode => Instance != null && Instance.debugEnemyOnly;
    // Aggiungi il singleton (serve per la property statica)
    public static EnemySpawner Instance { get; private set; }

    // Timer indipendenti per tipo
    private float fighterTimer;
    private float kamikazeTimer;
    private float bomberTimer;
    private float pulsarTimer;
    private float debugTimer;

    // Ultimi punti di spawn per fascia (per la distanza minima)
    private float lastTopSpawnX = float.MinValue;
    private float lastLeftSpawnY = float.MinValue;
    private float lastRightSpawnY = float.MinValue;

    // Range calcolati dalla camera
    private float topSpawnY;          // Y di spawn top (sopra camera)
    private float topSpawnMinX;       // X minima range top
    private float topSpawnMaxX;       // X massima range top

    private float sideSpawnMinY;      // Y minima range laterale (metà superiore camera)
    private float sideSpawnMaxY;      // Y massima range laterale (appena sopra camera)
    private float leftSpawnX;         // X di spawn sinistro
    private float rightSpawnX;        // X di spawn destro

    private DifficultyManager difficultyManager;
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        difficultyManager = DifficultyManager.Instance;
        CalculateSpawnRanges();

        if (debugEnemyOnly)
        {
            // Disabilita asteroidi per isolare il test
            AsteroidSpawner asteroidSpawner = FindFirstObjectByType<AsteroidSpawner>();
            if (asteroidSpawner != null) asteroidSpawner.enabled = false;

            Debug.Log("[EnemySpawner] DEBUG MODE attivo — asteroidi disabilitati.");
        }
    }

    void CalculateSpawnRanges()
    {
        Camera cam = Camera.main;
        float camDist = -cam.transform.position.z;

        Vector2 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, camDist));
        Vector2 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, camDist));
        Vector2 center = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, camDist));

        // TOP: appena sopra la camera, rientro orizzontale per non spawnare negli angoli
        topSpawnY = topRight.y + topSpawnMargin;
        topSpawnMinX = bottomLeft.x + topHorizontalInset;
        topSpawnMaxX = topRight.x - topHorizontalInset;

        // LATI: dalla metà verticale della camera fino ad appena sopra il bordo
        sideSpawnMinY = center.y;
        sideSpawnMaxY = topRight.y + sideSpawnMargin;

        leftSpawnX = bottomLeft.x - sideSpawnMargin;
        rightSpawnX = topRight.x + sideSpawnMargin;
    }

    void Update()
    {
        if (difficultyManager == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;

        if (debugEnemyOnly)
        {
            UpdateDebug();
            return; // bypassa completamente la logica normale
        }

        UpdateNormal();
    }

    // ── NORMAL MODE ───────────────────────────────────────────────────────────

    void UpdateNormal()
    {
        if (difficultyManager == null) return;

        PhaseConfig phase = difficultyManager
            .GetCurrentLevelProfile()
            .GetPhaseConfig(difficultyManager.GetCurrentPhase());

        if (phase.allowFighters) HandleFighterSpawn(phase);
        if (phase.allowKamikazes) HandleKamikazeSpawn(phase);
        if (phase.allowBombers) HandleBomberSpawn(phase);
        if (phase.allowPulsars) HandlePulsarSpawn(phase);
    }

    // ── DEBUG MODE ────────────────────────────────────────────────────────────

    void UpdateDebug()
    {
        debugTimer += Time.deltaTime;
        if (debugTimer < debugSpawnInterval) return;
        debugTimer = 0f;

        if (debugFighterOnly)
        {
            SpawnEnemy(fighterPrefabs, GetTopSpawnPosition());
        }
        else if (debugKamikazeOnly)
        {
            // Fix: sempre dall'alto, come in HandleKamikazeSpawn()
            SpawnEnemy(kamikazePrefabs, GetTopSpawnPosition());
        }
        else if (debugBomberOnly)
        {
            SpawnEnemy(bomberPrefabs, GetTopSpawnPosition());
        }
        else if (debugPulsarOnly)
        {
            // Pulsar: spawna sempre dall'alto per testare il comportamento completo
            SpawnEnemy(pulsarPrefabs, GetTopSpawnPosition());
        }
        else
        {
            // debugEnemyOnly senza tipo specifico → spawna tutti in sequenza
            SpawnEnemy(fighterPrefabs, GetTopSpawnPosition());
        }
    }

    // ──────────────────── SPAWN HANDLERS ────────────────────────────────────────────────────────
    // ── FIGHTER ──────────────────────────────────────────────────────────────
    // Entra sempre dall'alto
    void HandleFighterSpawn(PhaseConfig phase)
    {
        fighterTimer += Time.deltaTime; 
        if (fighterTimer < baseFighterInterval / phase.speedMultiplier) return; // non è ancora ora di spawnare

        Vector3 pos = GetTopSpawnPosition();
        SpawnEnemy(fighterPrefabs, pos);
        fighterTimer = 0f;
    }

    // ── KAMIKAZE ─────────────────────────────────────────────────────────────
    // tutti dall'alto hanno variazione di ingresso, quelli laterali entrano sempre a metà camera
    void HandleKamikazeSpawn(PhaseConfig phase)
    {
        kamikazeTimer += Time.deltaTime;
        if (kamikazeTimer < baseKamikazeInterval / phase.speedMultiplier) return;

        // Sempre dall'alto: la variazione di ingresso è già garantita dal range X
        SpawnEnemy(kamikazePrefabs, GetTopSpawnPosition());
        kamikazeTimer = 0f;
    }

    // ── BOMBER ───────────────────────────────────────────────────────────────
    // Entra sempre dall'alto, traversata lenta orizzontale
    void HandleBomberSpawn(PhaseConfig phase)
    {
        bomberTimer += Time.deltaTime;
        if (bomberTimer < baseBomberInterval / phase.speedMultiplier) return;

        Vector3 pos = GetTopSpawnPosition();
        SpawnEnemy(bomberPrefabs, pos);
        bomberTimer = 0f;
    }

    // ── PULSAR ───────────────────────────────────────────────────────────────
    // Entra sempre dall'alto, si posiziona e spara burst laser
    void HandlePulsarSpawn(PhaseConfig phase)
    {
        pulsarTimer += Time.deltaTime;
        if (pulsarTimer < basePulsarInterval / phase.speedMultiplier) return;

        SpawnEnemy(pulsarPrefabs, GetTopSpawnPosition());
        pulsarTimer = 0f;
    }

    // ── SPAWN POSITION HELPERS ────────────────────────────────────────────────

    Vector3 GetTopSpawnPosition()
    {
        float x = GetRandomWithMinDistance(
            topSpawnMinX, topSpawnMaxX,
            ref lastTopSpawnX,
            minSpawnDistance);

        return new Vector3(x, topSpawnY, 0f);
    }

    Vector3 GetSideSpawnPosition(bool fromLeft)
    {
        ref float lastY = ref (fromLeft ? ref lastLeftSpawnY : ref lastRightSpawnY);

        float y = GetRandomWithMinDistance(
            sideSpawnMinY, sideSpawnMaxY,
            ref lastY,
            minSpawnDistance);

        float x = fromLeft ? leftSpawnX : rightSpawnX;
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Ritorna un valore casuale in [min, max] garantendo una distanza minima
    /// dall'ultimo valore usato. Dopo maxRetries tentativi falliti accetta comunque.
    /// </summary>
    float GetRandomWithMinDistance(float min, float max, ref float lastValue, float minDist)
    {
        float candidate;
        int attempts = 0;

        do
        {
            candidate = Random.Range(min, max);
            attempts++;
        }
        while (Mathf.Abs(candidate - lastValue) < minDist && attempts < maxRetries);

        lastValue = candidate;
        return candidate;
    }

    // ── SPAWN ─────────────────────────────────────────────────────────────────

    void SpawnEnemy(GameObject[] prefabs, Vector3 position)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        Instantiate(prefab, position, prefab.transform.rotation);
    }
}