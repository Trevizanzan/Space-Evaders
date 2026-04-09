using UnityEngine;

/// <summary>
/// EnemyPulsar — navetta corazzata che si posiziona nella fascia alta della camera,
/// mira al player con un breve ritardo e scarica burst di laser sottili.
/// 
/// Stati:
///   Entering     → scende dall'alto verso la propria Y di patrol
///   Positioning  → si sposta orizzontalmente verso una X casuale
///   Aiming       → si ferma, ruota verso il player (effetto "mira")
///   Firing       → burst di N laser; dopo K burst consecutivi torna a Positioning
/// </summary>
public class EnemyPulsar : EnemyBase
{
    [Header("Pulsar Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float verticalSpeed = 3f;

    [Header("Patrol Y Range (% camera height, 0=centro, 1=bordo top)")]
    [SerializeField][Range(0f, 1f)] private float patrolMinYPercent = 0.55f;
    [SerializeField][Range(0f, 1f)] private float patrolMaxYPercent = 0.80f;
    [SerializeField] private float positionSlowRadius = 0.8f;

    [Header("Aiming")]
    [SerializeField] private float aimDuration = 0.3f;
    [SerializeField] private float aimRotationSpeed = 180f;

    [Header("Burst Settings")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int burstsBeforeReposition = 0; // burst consecutivi prima di muoversi

    // ── Stato ────────────────────────────────────────────────────────────────
    private enum PulsarState { Entering, Positioning, Aiming, Firing }
    private PulsarState state = PulsarState.Entering;

    private float targetY;
    private float targetX;
    private float minX, maxX;

    private float stateTimer;

    // Burst
    private Vector2 frozenAimDir;
    private int burstsThisCycle; // burst sparati dall'ultimo Positioning


    // ── Init ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();

        CameraBounds b = GetCameraBounds();

        minX = b.minX + 0.5f;
        maxX = b.maxX - 0.5f;

        float camTop = b.topY;
        targetY = Random.Range(camTop * patrolMinYPercent, camTop * patrolMaxYPercent);
    }

    // ── Dispatcher ───────────────────────────────────────────────────────────
    protected override void UpdateBehavior()
    {
        switch (state)
        {
            case PulsarState.Entering: UpdateEntering(); break;
            case PulsarState.Positioning: UpdatePositioning(); break;
            case PulsarState.Aiming: UpdateAiming(); break;
            //case PulsarState.Firing: UpdateFiring(); break;  // rimetti questa riga
        }
    }

    // ── ENTERING ─────────────────────────────────────────────────────────────
    void UpdateEntering()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, -90f);

        float newY = Mathf.MoveTowards(
            transform.position.y, targetY, verticalSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        if (Mathf.Abs(transform.position.y - targetY) < 0.1f)
            EnterPositioning();
    }

    // ── POSITIONING ───────────────────────────────────────────────────────────
    void UpdatePositioning()
    {
        float dist = Mathf.Abs(transform.position.x - targetX);
        float speed = dist < positionSlowRadius
            ? moveSpeed * (dist / positionSlowRadius)
            : moveSpeed;
        speed = Mathf.Max(speed, 0.3f);

        float newX = Mathf.MoveTowards(
            transform.position.x, targetX, speed * Time.deltaTime);
        transform.position = new Vector3(newX, targetY, transform.position.z);

        RotateTowardPlayer(aimRotationSpeed * 0.5f);

        if (Mathf.Abs(transform.position.x - targetX) < 0.05f)
            EnterAiming();
    }

    // ── AIMING ───────────────────────────────────────────────────────────────
    void UpdateAiming()
    {
        stateTimer += Time.deltaTime;
        RotateTowardPlayer(aimRotationSpeed);

        if (stateTimer >= aimDuration)
            EnterFiring();
    }
    // ── FIRING ───────────────────────────────────────────────────────────────
   

    // ── Transizioni ──────────────────────────────────────────────────────────
    void EnterPositioning()
    {
        burstsThisCycle = 0;

        float halfWidth = (maxX - minX) * 0.5f;
        if (transform.position.x > (minX + maxX) * 0.5f)
            targetX = Random.Range(minX, minX + halfWidth);
        else
            targetX = Random.Range(minX + halfWidth, maxX);

        state = PulsarState.Positioning;
    }

    void EnterAiming()
    {
        stateTimer = 0f;
        state = PulsarState.Aiming;
    }

    void EnterFiring()
    {
        if (playerTransform != null)
            frozenAimDir = (playerTransform.position - transform.position).normalized;
        else
            frozenAimDir = Vector2.down;

        FireLaser();

        burstsThisCycle++;
        if (burstsThisCycle < burstsBeforeReposition)
            EnterAiming();
        else
            EnterPositioning();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    void RotateTowardPlayer(float speed)
    {
        if (playerTransform == null) return;

        Vector2 dir = (playerTransform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, speed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    void FireLaser(float offset = 0f)
    {
        if (laserPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject laser = Instantiate(laserPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = laser.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            EnemyBullet bullet = laser.GetComponent<EnemyBullet>();
            float speed = bullet != null ? bullet.GetSpeed() : 22f;
            rb.linearVelocity = frozenAimDir * speed;
        }

        float angle = Mathf.Atan2(frozenAimDir.y, frozenAimDir.x) * Mathf.Rad2Deg;
        laser.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShoot();
    }
}