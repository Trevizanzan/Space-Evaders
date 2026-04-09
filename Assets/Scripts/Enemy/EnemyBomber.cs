// Assets/Scripts/Enemy/EnemyBomber.cs
using UnityEngine;

public class EnemyBomber : EnemyBase
{
    [Header("Bomber Movement")]
    [SerializeField] private float moveSpeed = 2f;    // velocità traversata orizzontale
    [SerializeField] private float patrolMinY = 3f;    // Y minima patrol (terzo superiore camera)
    [SerializeField] private float patrolMaxY = 5f;    // Y massima patrol (appena sotto il bordo)
    [SerializeField] private float verticalSpeed = 1f;    // velocità aggiustamento Y
    [SerializeField][Range(0f, 1f)] private float patrolMinYPercent = 0.55f; // % altezza camera
    [SerializeField][Range(0f, 1f)] private float patrolMaxYPercent = 0.80f; // % altezza camera

    [Header("Bomb Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float bombIntervalMin = 1.5f;
    [SerializeField] private float bombIntervalMax = 3f;
    [SerializeField] private Transform firePoint;

    // Stato
    private enum BomberState { Entering, Patrolling }
    private BomberState state = BomberState.Entering;

    private float targetY;          // Y di patrol assegnata allo spawn
    private float moveDirection;    // 1 = destra, -1 = sinistra
    private float minX, maxX;       // limiti orizzontali camera

    private float bombTimer;
    private float currentBombInterval;

    protected override void Start()
    {
        base.Start();

        CameraBounds b = GetCameraBounds();
        minX = b.minX + 0.5f;
        maxX = b.maxX - 0.5f;

        float camHeight = b.topY - b.minY;
        float patrolMinY = b.minY + camHeight * patrolMinYPercent;
        float patrolMaxY = b.minY + camHeight * patrolMaxYPercent;
        targetY = Random.Range(patrolMinY, patrolMaxY);

        // Direzione iniziale: se spawna a sinistra va a destra, e viceversa
        moveDirection = transform.position.x < 0 ? 1f : -1f;

        currentBombInterval = Random.Range(bombIntervalMin, bombIntervalMax);
    }

    protected override void UpdateBehavior()
    {
        switch (state)
        {
            case BomberState.Entering:
                UpdateEntering();
                break;
            case BomberState.Patrolling:
                UpdatePatrolling();
                break;
        }
    }

    // ── ENTERING ─────────────────────────────────────────────────────────────
    // Scende fino alla Y di patrol, poi inizia il patrol
    void UpdateEntering()
    {
        // Orientamento fisso verso il basso
        transform.rotation = Quaternion.Euler(0f, 0f, -90f);

        float newY = Mathf.MoveTowards(transform.position.y, targetY, verticalSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        if (Mathf.Abs(transform.position.y - targetY) < 0.1f)
        {
            state = BomberState.Patrolling;
        }
    }

    // ── PATROLLING ────────────────────────────────────────────────────────────
    // Traversata orizzontale ping-pong, sgancia bombe a intervalli
    void UpdatePatrolling()
    {
        // Movimento orizzontale
        float newX = transform.position.x + moveDirection * moveSpeed * Time.deltaTime;

        // Rimbalza sui bordi
        if (newX >= maxX)
        {
            newX = maxX;
            moveDirection = -1f;
        }
        else if (newX <= minX)
        {
            newX = minX;
            moveDirection = 1f;
        }

        transform.position = new Vector3(newX, targetY, transform.position.z);

        // Sgancia bombe
        HandleBombing();
    }

    void HandleBombing()
    {
        bombTimer += Time.deltaTime;
        if (bombTimer < currentBombInterval) return;

        DropBomb();
        bombTimer = 0f;
        currentBombInterval = Random.Range(bombIntervalMin, bombIntervalMax);
    }

    void DropBomb()
    {
        if (bombPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        // La bomba cade sempre verso il basso, indipendentemente dalla direzione del bomber
        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = bomb.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.down * bomb.GetComponent<EnemyBullet>().GetSpeed();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShoot(); // TODO: suono dedicato bomba
    }
}