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

    [Header("Railgun Charge Effect")]
    [SerializeField] private GameObject chargeEffectPrefab;

    private WeaponData currentWeapon;
    private float lastShootTime;
    private bool shootingDisabled;

    // Stato carica per armi con requiresCharging (es. Railgun)
    private bool isCharging;
    private float chargeStartTime;
    private GameObject activeChargeEffect;

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

        if (currentWeapon.autoFire && currentWeapon.requiresCharging)
            HandleAutoChargingFire();
        else if (currentWeapon.autoFire)
        {
            if (Time.time - lastShootTime >= currentWeapon.shootCooldown)
                Shoot();
        }
        else if (currentWeapon.requiresCharging)
            HandleChargingFire(fireHeld);
        else
        {
            if (fireHeld && Time.time - lastShootTime >= currentWeapon.shootCooldown)
                Shoot();
        }
    }

    // Carica automaticamente, mostra l'effetto e spara quando pronto, poi riparte
    void HandleAutoChargingFire()
    {
        if (!isCharging)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            SpawnChargeEffect();
        }

        if (Time.time - chargeStartTime >= currentWeapon.chargeTime)
        {
            CancelCharge();
            Shoot();
        }
    }

    // Logica di carica: tieni premuto → l'effetto cresce → rilascia per sparare
    void HandleChargingFire(bool fireHeld)
    {
        if (fireHeld && !isCharging)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            SpawnChargeEffect();
        }

        if (!fireHeld && isCharging)
        {
            bool fullyCharged = (Time.time - chargeStartTime) >= currentWeapon.chargeTime;
            CancelCharge();
            if (fullyCharged)
                Shoot();
        }
    }

    void SpawnChargeEffect()
    {
        if (chargeEffectPrefab == null || firePoint == null) return;
        activeChargeEffect = Instantiate(chargeEffectPrefab, firePoint.position, firePoint.rotation, firePoint);
        if (activeChargeEffect.TryGetComponent<ChargeEffect>(out var fx))
            fx.Initialize(currentWeapon.chargeTime);
    }

    void CancelCharge()
    {
        isCharging = false;
        if (activeChargeEffect != null)
        {
            Destroy(activeChargeEffect);
            activeChargeEffect = null;
        }
    }

    void Shoot()
    {
        currentWeapon.Fire(firePoint);
        if (RunStats.Instance != null) RunStats.Instance.RegisterShotFired();
        lastShootTime = Time.time;
    }

    public void SetShootingDisabled(bool disabled)
    {
        shootingDisabled = disabled;
        if (disabled) CancelCharge(); // interrompe carica se il player viene bloccato
    }

    // Usato dal futuro menu di selezione arma (task #4) o per swap runtime
    public void EquipWeapon(WeaponData weapon)
    {
        CancelCharge();
        currentWeapon = weapon;
    }
}
