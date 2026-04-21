using UnityEngine;

[CreateAssetMenu(menuName = "Space Evader/Weapons/Spread Gun", fileName = "SpreadGun")]
public class SpreadGunData : WeaponData
{
    [Header("Spread Settings")]
    [Min(1)] public int bulletCount = 3;
    [Range(5f, 90f)] public float spreadAngle = 30f;

    public override void Fire(Transform firePoint)
    {
        float halfSpread = spreadAngle / 2f;
        float step = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;
        for (int i = 0; i < bulletCount; i++)
        {
            float offset = halfSpread - step * i;  // da +half (sinistra) a -half (destra)
            // 90° = rotazione base del prefab (sprite verso l'alto) + offset di spread
            Quaternion rotation = Quaternion.Euler(0, 0, 90f + offset);
            var go = Instantiate(projectilePrefab, firePoint.position, rotation);
            if (go.TryGetComponent<PlayerBullet>(out var bullet))
                bullet.Initialize(damage);
        }
    }
}
