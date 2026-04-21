using UnityEngine;

// GDD target: requiresCharging=true, chargeTime=1s, shootCooldown=1.5s, damage=3
[CreateAssetMenu(menuName = "Space Evader/Weapons/Railgun", fileName = "Railgun")]
public class RailgunData : WeaponData
{
    public override void Fire(Transform firePoint)
    {
        var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        if (go.TryGetComponent<PlayerBullet>(out var bullet))
            bullet.Initialize(damage, piercing: true);
    }
}
