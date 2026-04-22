using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "Weapon";
    [TextArea(2, 4)] public string description = "";

    [Header("Fire Behavior")]
    public bool autoFire = false;
    public float shootCooldown = 0.15f;
    public bool requiresCharging = false;
    [Tooltip("Ignorato se requiresCharging è false")]
    public float chargeTime = 1f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public int damage = 1;

    // Spawna il/i proiettile/i. Chiamato da PlayerShooting quando le condizioni di fuoco sono soddisfatte.
    public abstract void Fire(Transform firePoint);
}
