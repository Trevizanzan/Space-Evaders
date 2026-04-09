// Assets/Scripts/Enemy/EnemyKamikaze.cs
using UnityEngine;

public class EnemyKamikaze : EnemyBase
{
    [Header("Kamikaze Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 200f;  // gradi/secondo verso il player
    [SerializeField] private float accelerationTime = 1.5f;  // secondi prima di raggiungere velocità massima

    [Header("Charge Settings")]
    [SerializeField] private float chargeDelay = 0.8f;  // secondi di "pausa" prima di caricare
    [SerializeField] private float chargeSpeedBoost = 2.5f;  // moltiplicatore velocità durante la carica

    // Stato
    private enum KamikazeState { Entering, Hovering, Charging }
    private KamikazeState state = KamikazeState.Entering;

    private float stateTimer = 0f;
    private float currentSpeed = 0f;
    private Vector3 chargeDir;         // direzione congelata al momento della carica

    protected override void Start()
    {
        base.Start();
        // playerTransform già popolato da EnemyBase.Start()
    }

    protected override void UpdateBehavior()
    {
        switch (state)
        {
            case KamikazeState.Entering:
                UpdateEntering();
                break;
            case KamikazeState.Hovering:
                UpdateHovering();
                break;
            case KamikazeState.Charging:
                UpdateCharging();
                break;
        }
    }

    // ── ENTERING ─────────────────────────────────────────────────────────────
    // Scende lentamente verso il centro della camera, poi si ferma brevemente
    void UpdateEntering()
    {
        // Scende verso Y = 0 (centro camera) a velocità ridotta
        float targetY = 2f; // si ferma a circa 2 unità sopra il centro
        float newY = Mathf.MoveTowards(transform.position.y, targetY, moveSpeed * 0.5f * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        RotateTowardPlayer();

        // Quando ha raggiunto la posizione di hovering, passa allo stato successivo
        if (Mathf.Abs(transform.position.y - targetY) < 0.1f)
        {
            state = KamikazeState.Hovering;
            stateTimer = 0f;
        }
    }

    // ── HOVERING ─────────────────────────────────────────────────────────────
    // Si ferma un momento puntando il player, poi carica
    void UpdateHovering()
    {
        stateTimer += Time.deltaTime;

        RotateTowardPlayer();

        // Piccolo drift laterale per sembrare vivo mentre aspetta
        float drift = Mathf.Sin(stateTimer * 3f) * 0.5f;
        transform.position += new Vector3(drift * Time.deltaTime, 0f, 0f);

        if (stateTimer >= chargeDelay)
        {
            // Congela la direzione verso il player al momento della carica
            if (playerTransform != null)
                chargeDir = (playerTransform.position - transform.position).normalized;
            else
                chargeDir = Vector3.down;

            state = KamikazeState.Charging;
            stateTimer = 0f;
            currentSpeed = 0f;
        }
    }

    // ── CHARGING ─────────────────────────────────────────────────────────────
    // Carica in linea retta verso la direzione congelata, accelerando
    void UpdateCharging()
    {
        stateTimer += Time.deltaTime;

        // Accelerazione progressiva fino a velocità massima
        float t = Mathf.Clamp01(stateTimer / accelerationTime);
        currentSpeed = Mathf.Lerp(0f, moveSpeed * chargeSpeedBoost, t);

        transform.position += chargeDir * currentSpeed * Time.deltaTime;

        // Rotazione fissa nella direzione di carica (non segue più il player)
        float targetAngle = Mathf.Atan2(chargeDir.y, chargeDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    void RotateTowardPlayer()
    {
        if (playerTransform == null) return;

        Vector2 dir = (playerTransform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.z;

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }
}