using UnityEngine;

/// <summary>
/// EnemyPulsar — navetta corazzata che si posiziona nella fascia alta della camera,
/// mira al player con un breve ritardo e scarica un burst di laser sottili.
/// 
/// Stati:
///   Entering    → scende dall'alto verso la propria Y di patrol
///   Positioning → si sposta orizzontalmente verso una X casuale (rallenta in avvicinamento)
///   Aiming      → si ferma, ruota verso il player (effetto "mira")
///   Firing      → burst di N laser in rapida successione, poi torna a Positioning
/// </summary>
public class EnemyPulsar : EnemyBase
{
    [Header("Pulsar Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float verticalSpeed = 3f;

    [Header("Patrol Y Range (% dal bordo superiore, 0 = top, 1 = fondo schermo)")]
    [SerializeField][Range(0f, 1f)] private float patrolMinYPercent = 0.10f; // appena sotto il top (% camera)
    [SerializeField][Range(0f, 1f)] private float patrolMaxYPercent = 0.35f; // terzo superiore (% camera)
    [SerializeField] private float positionSlowRadius = 1.2f;   // distanza entro cui inizia a rallentare

    //[SerializeField][Range(0f, 1f)] private float patrolMinYPercent = 0.55f; // % altezza camera
    //[SerializeField][Range(0f, 1f)] private float patrolMaxYPercent = 0.80f; // % altezza camera
    //[SerializeField] private float positionSlowRadius = 1.2f;    

    [Header("Aiming")]
    [SerializeField] private float aimDuration = 0.5f;    // secondi di pausa prima di sparare
    [SerializeField] private float aimRotationSpeed = 180f; // gradi/secondo durante la mira

    [Header("Burst Settings")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int burstCount = 4;          // laser per burst
    [SerializeField] private float burstInterval = 0.12f; // secondi tra un laser e il successivo
    [SerializeField] private float cooldownAfterBurst = 0.6f; // pausa dopo il burst prima di riposizionarsi

    // ── Stato ────────────────────────────────────────────────────────────────
    private enum PulsarState { Entering, Positioning, Aiming, Firing }
    private PulsarState state = PulsarState.Entering;

    private float targetY;           // Y di patrol fissa
    private float targetX;           // X verso cui si sta spostando
    private float minX, maxX;        // limiti orizzontali camera

    private float stateTimer;        // timer generico per gli stati temporizzati

    // Burst
    private int burstShotsFired;     // laser già sparati nel burst corrente
    private float burstTimer;        // timer tra un colpo e l'altro
    private Vector2 frozenAimDir;    // direzione congelata al momento dello sparo

    private Transform playerTransform;

    // ── Init ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();

        CameraBounds b = GetCameraBounds(); // un'unica chiamata centralizzata
        
        minX = b.minX + 0.5f;
        maxX = b.maxX - 0.5f;

        // Range calcolato dal bordo superiore verso il basso
        float camHeight = b.topY - b.minY;
        float patrolMinY = b.topY - camHeight * patrolMaxYPercent; // max scende di più
        float patrolMaxY = b.topY - camHeight * patrolMinYPercent; // min scende di meno

        targetY = Random.Range(patrolMinY, patrolMaxY);
    }

    // ── Dispatcher ───────────────────────────────────────────────────────────
    protected override void UpdateBehavior()
    {
        switch (state)
        {
            case PulsarState.Entering: UpdateEntering(); break;
            case PulsarState.Positioning: UpdatePositioning(); break;
            case PulsarState.Aiming: UpdateAiming(); break;
            case PulsarState.Firing: UpdateFiring(); break;
        }
    }

    // ── ENTERING ─────────────────────────────────────────────────────────────
    // Scende verticalmente fino alla Y di patrol, orientato verso il basso
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
    // Si sposta verso targetX rallentando quando si avvicina (ease-out)
    void UpdatePositioning()
    {
        float dist = Mathf.Abs(transform.position.x - targetX);
        float speed = dist < positionSlowRadius
            ? moveSpeed * (dist / positionSlowRadius)   // rallenta in avvicinamento
            : moveSpeed;
        speed = Mathf.Max(speed, 0.3f);                 // velocità minima per non bloccarsi

        float newX = Mathf.MoveTowards(
            transform.position.x, targetX, speed * Time.deltaTime);
        transform.position = new Vector3(newX, targetY, transform.position.z);

        // Ruota verso il player anche mentre si sposta (effetto "tracciamento")
        RotateTowardPlayer(aimRotationSpeed * 0.5f);

        if (Mathf.Abs(transform.position.x - targetX) < 0.05f)
            EnterAiming();
    }

    // ── AIMING ───────────────────────────────────────────────────────────────
    // Fermo, ruota velocemente verso il player per aimDuration secondi
    void UpdateAiming()
    {
        stateTimer += Time.deltaTime;
        RotateTowardPlayer(aimRotationSpeed);

        if (stateTimer >= aimDuration)
            EnterFiring();
    }

    // ── FIRING ───────────────────────────────────────────────────────────────
    // Spara un burst di burstCount laser, poi aspetta cooldown e si riposiziona
    void UpdateFiring()
    {
        stateTimer += Time.deltaTime;

        // Fase sparo: emette i colpi a intervalli regolari
        if (burstShotsFired < burstCount)
        {
            burstTimer += Time.deltaTime;
            if (burstTimer >= burstInterval)
            {
                FireLaser();
                burstShotsFired++;
                burstTimer = 0f;
            }
            return;
        }

        // Tutti i colpi sparati → aspetta il cooldown poi torna a Positioning
        if (stateTimer >= burstCount * burstInterval + cooldownAfterBurst)
            EnterPositioning();
    }

    // ── Transizioni ──────────────────────────────────────────────────────────
    void EnterPositioning()
    {
        // Sceglie una X casuale nella metà opposta rispetto alla posizione attuale
        // per garantire spostamenti visibili e non triviali
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
        // Congela la direzione di mira al momento in cui parte il burst
        if (playerTransform != null)
            frozenAimDir = (playerTransform.position - transform.position).normalized;
        else
            frozenAimDir = Vector2.down;

        burstShotsFired = 0;
        burstTimer = burstInterval; // spara il primo colpo subito
        stateTimer = 0f;
        state = PulsarState.Firing;
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

    void FireLaser()
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

        // Ruota il laser visivamente nella direzione di sparo
        float angle = Mathf.Atan2(frozenAimDir.y, frozenAimDir.x) * Mathf.Rad2Deg;
        laser.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShoot(); // TODO: suono dedicato laser Pulsar
    }
}
