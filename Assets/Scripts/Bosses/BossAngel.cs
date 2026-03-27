using UnityEngine;
using UnityEngine.UI;

public class BossAngel : BossBase
{
    [Header("BossAngel Specifics")]
    [SerializeField] private float shootInterval = 1f;
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float cameraEdgeOffset = 0.25f; // Distanza dal bordo camera (.5 è un quadrattino)
    [SerializeField] private float centerY = -1f; // Quanto scende (appena sopra il centro)
    [SerializeField] private float timeAtCenter = 5f; // Quanto tempo fa avanti/indietro al centro

    private float targetX; // Posizione X target
    private float shootTimer;
    private float minX; // Limite sinistro
    private float maxX; // Limite destro
    private float startY; // Posizione Y di partenza (dove arriva l'entrata)

    private bool isMovingPattern = false; // Se sta facendo il pattern movimento

    protected override void OnEntranceComplete()
    {
        // Calcola i limiti orizzontali della camera
        float cameraWidth = Camera.main.orthographicSize * Camera.main.aspect;
        minX = -cameraWidth + cameraEdgeOffset;
        maxX = cameraWidth - cameraEdgeOffset;

        // Salva la posizione Y dove è arrivato (inizio del pattern)
        startY = transform.position.y;

        // Parte dal centro (dove è finita l'entrata)
        targetX = transform.position.x;

        // Scegli subito un nuovo target random
        ChooseNewTarget();
    }

    /// <summary>
    /// Gestisce movimento e attacco del boss.
    /// Movimento: il boss si muove orizzontalmente avanti e indietro (ping-pong) attorno a un punto centrale.
    /// </summary>
    protected override void UpdateBehavior()
    {
        // 1) MOVIMENTO
        // Movimento verso il target X
        float currentX = transform.position.x;

        // Se ha raggiunto il target (o è molto vicino), scegline uno nuovo
        if (Mathf.Abs(currentX - targetX) < 0.1f)
        {
            ChooseNewTarget();
        }

        // Muovi verso il target
        float newX = Mathf.MoveTowards(currentX, targetX, moveSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        // 2) ATTACCO
        // Pattern attacco: spara verso il basso ogni N secondi
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            Shoot();
            shootTimer = 0f;
        }
    }

    void ChooseNewTarget()
    {
        // Sceglie un X random entro i limiti della camera
        targetX = Random.Range(minX, maxX);
    }

    void Shoot()
    {
        if (enemyBulletPrefab == null) return;

        // Spawna proiettile sotto il boss
        Vector3 spawnPos = transform.position + Vector3.down * 0.5f;

        // usa la rotazione del prefab per orientare correttamente il proiettile
        GameObject bullet = Instantiate(enemyBulletPrefab, spawnPos, enemyBulletPrefab.transform.rotation);

        // Suono (opzionale)
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShoot();
    }

    protected override void OnDamageFeedback()
    {
        // Flash bianco veloce (opzionale)
        //StartCoroutine(FlashWhite());

        // Spawna esplosione davanti al boss (z negativo = più avanti)
        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);
        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.SpawnSmall(explosionPos, 1f);
        }
    }

    System.Collections.IEnumerator FlashWhite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogWarning("[FLASH] NO SpriteRenderer found!");
            yield break;
        }

        Color originalColor = sr.color;

        // Flash bianco
        sr.color = Color.cyan;
        yield return new WaitForSeconds(0.15f);

        // Ripristina il colore
        sr.color = originalColor;

    }
}