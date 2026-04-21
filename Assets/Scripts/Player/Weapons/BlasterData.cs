using UnityEngine;

[CreateAssetMenu(menuName = "Space Evader/Weapons/Blaster", fileName = "Blaster")]
public class BlasterData : WeaponData
{
    public override void Fire(Transform firePoint)
    {
        var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        if (go.TryGetComponent<PlayerBullet>(out var bullet))
            bullet.Initialize(damage);
    }
}
