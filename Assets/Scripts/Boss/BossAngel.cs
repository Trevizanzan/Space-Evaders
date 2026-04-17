using UnityEngine;
using System.Collections;

public class BossAngel : BossBase
{
    [Header("BossAngel Specifics")]
    [SerializeField] private float shootIntervalMin = 0.25f;
    [SerializeField] private float shootIntervalMax = 0.8f;
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float cameraEdgeOffset = .25f;    // Distanza dal bordo camera

    [Header("Vertical Movement (% camera)")]
    [SerializeField][Range(0f, 1f)] private float topYPercent = 0.8f;    // 80% del bordo superiore
    [SerializeField][Range(0f, 1f)] private float centerYPercent = 0.1f; // 10% sopra il centro
    [SerializeField] private float timeAtCenterMin = 2f;
    [SerializeField] private float timeAtCenterMax = 5f;

    // Calcolati a runtime in OnEntranceComplete
    private float topY;     // Posizione Y del livello superiore (dove arriva l'entrata)
    private float centerY;  // Posizione Y del livello inferiore (dove scende)
    private float targetX;
    private float targetY;
    private float shootTimer;
    private float currentShootInterval; // Intervallo corrente per lo sparo, che varia ogni volta
    private float minX; // limite sinistro
    private float maxX; // limite destro
    private float startY;   // posizione Y di partenza (dove arriva l'entrata)

    protected override void Start()  // ← "protected override", non "void"
    {
        bossDisplayName = "The Angel";
        base.Start();   // esegue tutto lo Start() di BossBase
    }

    protected override void OnEntranceComplete()
    {
        Camera cam = Camera.main;
        float cameraTop = cam.orthographicSize;
        float cameraWidth = cameraTop * cam.aspect;

        // topY rispetta il padding UI — calcolato da BossBase
        topY = cameraTop * topYPercent - TopUIWorldHeight;
        centerY = cameraTop * centerYPercent;

        minX = -cameraWidth + cameraEdgeOffset;
        maxX = cameraWidth - cameraEdgeOffset;

        startY = topY;
        targetY = startY;
        targetX = transform.position.x;

        ChooseNewXTarget();
        ChooseNewShootInterval();
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
            SoundManager.Instance.PlayEnemyShoot();
    }

    protected override void OnDamageFeedback()
    {
        FlashWhite(); // ereditato da EnemyBase/BossBase

        //Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);
        //if (ExplosionManager.Instance != null)
        //{
        //    ExplosionManager.Instance.SpawnSmall(explosionPos, 1f);
        //}
    }
}