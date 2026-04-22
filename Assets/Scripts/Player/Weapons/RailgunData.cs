using UnityEngine;

// GDD target: requiresCharging=true, chargeTime=1s, shootCooldown=1.5s, damage=3
[CreateAssetMenu(menuName = "Space Evader/Weapons/Railgun", fileName = "Railgun")]
public class RailgunData : WeaponData
{
    public override void Fire(Transform firePoint)
    {
        // Usa la rotazione base del prefab (90° Z) per direzione verso l'alto
        var go = Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        if (go.TryGetComponent<PlayerBullet>(out var bullet))
            bullet.Initialize(damage, piercing: true);
    }
}
