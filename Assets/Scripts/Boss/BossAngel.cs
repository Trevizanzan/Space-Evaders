using UnityEngine;
using System.Collections;

public class BossAngel : BossBase
{
    [Header("BossAngel Specifics")]
    [SerializeField] private float shootIntervalMin = 0.25f;
    [SerializeField] private float shootIntervalMax = 0.8f;
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float cameraEdgeOffset = .25f;    // Distanza dal bordo camera (.5 è un quadrattino)
    [SerializeField] private float topY = 5f; //  LIVELLO SUPERIORE (controllabile)
    [SerializeField] private float centerY = -1f;   // Quanto scende (appena sopra il centro) // LIVELLO INFERIORE (metà camera)
    [SerializeField] private float timeAtCenterMin = 2f;
    [SerializeField] private float timeAtCenterMax = 5f;

    private float targetX;
    private float targetY;
    private float shootTimer;
    private float currentShootInterval; // Intervallo corrente (varia)
    private float minX; // limite sinistro
    private float maxX; // limite destro
    private float startY;   // Posizione Y di partenza (dove arriva l'entrata)

    void Start()
    {
        bossDisplayName = "The Angel";
        base.Start();
    }

    protected override void OnEntranceComplete()
    {
        // Calcola i limiti orizzontali della camera
        float cameraWidth = Camera.main.orthographicSize * Camera.main.aspect;
        minX = -cameraWidth + cameraEdgeOffset;
        maxX = cameraWidth - cameraEdgeOffset;

        // Salva la posizione iniziale dove è arrivato dopo l'entrata, da cui partirà il pattern di movimento
        startY = topY;
        targetY = startY;
        targetX = transform.position.x;

        // Scegli subito un nuovo target random
        ChooseNewXTarget();
        ChooseNewShootInterval(); // Inizializza il primo intervallo

        StartCoroutine(VerticalMovementPattern());
    }

    // Pattern verticale: scende → pausa → risale → pausa → ripete
    IEnumerator VerticalMovementPattern()
    {
        while (!isDead)
        {
            // Scende
            targetY = centerY;
            yield return new WaitUntil(() => Mathf.Abs(transform.position.y - centerY) < 0.1f);

            // Rimane giù per un tempo random
            float timeDown = Random.Range(timeAtCenterMin, timeAtCenterMax);
            yield return new WaitForSeconds(timeDown);

            // Risale
            targetY = startY;
            yield return new WaitUntil(() => Mathf.Abs(transform.position.y - startY) < 0.1f);

            // Rimane su per un tempo random
            float timeUp = Random.Range(timeAtCenterMin, timeAtCenterMax);
            yield return new WaitForSeconds(timeUp);
        }
    }

    /// <summary>
    /// Gestisce movimento e attacco del boss.
    /// Movimento: il boss si muove orizzontalmente avanti e indietro (ping-pong) attorno a un punto centrale.
    /// </summary>
    protected override void UpdateBehavior()
    {
        // 1) MOVIMENTO X random
        float currentX = transform.position.x;

        // Se ha raggiunto il target (o è molto vicino), scegline uno nuovo
        if (Mathf.Abs(currentX - targetX) < 0.1f)
        {
            ChooseNewXTarget();
        }

        float newX = Mathf.MoveTowards(currentX, targetX, moveSpeed * Time.deltaTime);

        // Movimento Y verso targetY a velocità moveSpeed
        float currentY = transform.position.y;
        float newY = Mathf.MoveTowards(currentY, targetY, moveSpeed * Time.deltaTime);

        transform.position = new Vector3(newX, newY, transform.position.z);

        // 2) ATTACCO
        // Pattern attacco: spara verso il basso ogni N secondi
        shootTimer += Time.deltaTime;
        if (shootTimer >= currentShootInterval)
        {
            Shoot();
            shootTimer = 0f;
            ChooseNewShootInterval(); // Cambia intervallo ogni colpo
        }
    }

    void ChooseNewXTarget()
    {
        // Sceglie un X random entro i limiti della camera
        targetX = Random.Range(minX, maxX);
    }

    // Sceglie un nuovo intervallo random per lo sparo
    void ChooseNewShootInterval()
    {
        currentShootInterval = Random.Range(shootIntervalMin, shootIntervalMax);
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
        //StartCoroutine(FlashCyan());

        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);
        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.SpawnSmall(explosionPos, 1f);
        }
    }

    System.Collections.IEnumerator FlashCyan()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            yield break;
        }

        Color originalColor = sr.color;
        sr.color = Color.cyan;
        yield return new WaitForSeconds(0.15f);
        sr.color = originalColor;
    }
}