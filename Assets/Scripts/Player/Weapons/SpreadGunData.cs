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
        // Base rotation del prefab (es. 90° per Player_Bullet): rispettata per qualsiasi prefab
        Quaternion baseRotation = projectilePrefab.transform.rotation;
        for (int i = 0; i < bulletCount; i++)
        {
            float offset = halfSpread - step * i;  // da +half (sinistra) a -half (destra)
            Quaternion rotation = baseRotation * Quaternion.Euler(0, 0, offset);
            var go = Instantiate(projectilePrefab, firePoint.position, rotation);
            if (go.TryGetComponent<PlayerBullet>(out var bullet))
                bullet.Initialize(damage);
        }
    }
}
