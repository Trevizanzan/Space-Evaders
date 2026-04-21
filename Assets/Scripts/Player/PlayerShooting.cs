using UnityEngine;

/// <summary>
/// Gestisce QUANDO sparare in base all'input e all'arma equipaggiata.
/// La logica di COSA spawna è delegata al WeaponData corrente.
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponData defaultWeapon;
    public Transform firePoint;

    private WeaponData currentWeapon;
    private float lastShootTime;
    private bool shootingDisabled;

    // Stato carica per armi con requiresCharging (es. Railgun)
    private bool isCharging;
    private float chargeStartTime;

    private SpaceEvaderInputActions inputActions;

    void Awake()
    {
        inputActions = new SpaceEvaderInputActions();
        inputActions.Player.Enable();
        // Il menu pre-run (task #4) setta WeaponSelection.CurrentWeapon; fallback al default
        currentWeapon = WeaponSelection.CurrentWeapon ?? defaultWeapon;
    }

    void OnDestroy()
    {
        inputActions?.Disable();
        inputActions?.Dispose();
    }

    void Update()
    {
        if (shootingDisabled || BossBase.IsBossEntering || currentWeapon == null) return;
        HandleFire();
    }

    void HandleFire()
    {
        bool fireHeld = inputActions.Player.Fire.IsPressed();

        if (currentWeapon.requiresCharging)
            HandleChargingFire(fireHeld);
        else if (currentWeapon.autoFire)
        {
            if (Time.time - lastShootTime >= currentWeapon.shootCooldown)
                Shoot();
        }
        else
        {
            if (fireHeld && Time.time - lastShootTime >= currentWeapon.shootCooldown)
                Shoot();
        }
    }

    // Logica di carica: tieni premuto per caricare, rilascia per sparare (se carica completa)
    void HandleChargingFire(bool fireHeld)
    {
        if (fireHeld && !isCharging)
        {
            isCharging = true;
            chargeStartTime = Time.time;
        }

        if (!fireHeld && isCharging)
        {
            isCharging = false;
            bool fullyCharged = (Time.time - chargeStartTime) >= currentWeapon.chargeTime;
            bool cooldownReady = (Time.time - lastShootTime) >= currentWeapon.shootCooldown;
            if (fullyCharged && cooldownReady)
                Shoot();
        }
    }

    void Shoot()
    {
        currentWeapon.Fire(firePoint);
        if (RunStats.Instance != null) RunStats.Instance.RegisterShotFired();
        lastShootTime = Time.time;
    }

    public void SetShootingDisabled(bool disabled) => shootingDisabled = disabled;

    // Usato dal futuro menu di selezione arma (task #4) o per swap runtime
    public void EquipWeapon(WeaponData weapon)
    {
        currentWeapon = weapon;
        isCharging = false;
    }
}
