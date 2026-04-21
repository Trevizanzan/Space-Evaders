using UnityEngine;

[CreateAssetMenu(menuName = "Space Evader/Weapons/Blaster", fileName = "Blaster")]
public class BlasterData : WeaponData
{
    public override void Fire(Transform firePoint)
    {
        // Usa la rotazione del prefab (90° Z) così lo sprite guarda verso l'alto
        // e transform.right = (0,1) = verso l'alto (usato da PlayerBullet.Awake)
        var go = Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        if (go.TryGetComponent<PlayerBullet>(out var bullet))
            bullet.Initialize(damage);
    }
}
