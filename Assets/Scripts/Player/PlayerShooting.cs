using UnityEngine;

/// <summary>
/// questa classe si occupa solamente di gestire lo sparo del proiettile quando il giocatore preme il tasto di fuoco. (QUANDO e DOVE sparare)
/// non si occupa della logica del proiettile, che è gestita da un altro script attaccato al prefab del proiettile stesso.  (COME si comporta il proiettile)
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;  // slot per il prefab
    public Transform firePoint;      // punto dove spawna (davanti alla nave)

    [Header("Auto Fire")]
    [SerializeField] private bool autoFire = false;

    [SerializeField] private float shootCooldown = 0.13f; // tra uno sparo e l'altro devono passare almeno 0.13 secondi (8 spari al secondo)

    // Aggiunta cooldown per evitare di sparare troppo velocemente
    private float lastShootTime = 0f;

    private bool shootingDisabled = false;
    public void SetShootingDisabled(bool disabled)
    {
        shootingDisabled = disabled;
    }

    // Input System
    private SpaceEvaderInputActions inputActions;

    void Awake()
    {
        // Initialize Input System
        inputActions = new SpaceEvaderInputActions();
        inputActions.Player.Enable();
    }

    void OnDestroy()
    {
        // Clean up Input System
        inputActions?.Disable();
        inputActions?.Dispose();
    }

    void Update()
    {
        if (shootingDisabled) return;

        // Il player non può sparare se il boss sta entrando
        if (BossBase.IsBossEntering) return;

        bool firePressed = inputActions.Player.Fire.IsPressed();

        bool manualFire = firePressed && Time.time - lastShootTime >= shootCooldown;

        bool shouldShoot = autoFire
            ? Time.time - lastShootTime >= shootCooldown
            : manualFire;

        if (shouldShoot)
        {
            Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);

            // SOUND for shooting (uncomment if you have a SoundManager)
            //if (SoundManager.Instance != null)
            //    SoundManager.Instance.PlayShoot();

            lastShootTime = Time.time;
        }
    }
}