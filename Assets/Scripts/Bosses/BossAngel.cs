using UnityEngine;
using UnityEngine.UI;

public class BossAngel : BossBase
{
    [Header("BossAngel Specifics")]
    [SerializeField] private float patrolWidth = .5f;
    [SerializeField] private float shootInterval = 1f;
    [SerializeField] private GameObject enemyBulletPrefab;

    private Vector3 patrolCenter;
    private float shootTimer;

    protected override void OnEntranceComplete()
    {
        patrolCenter = transform.position;
    }

    /// <summary>
    /// Movimento: il boss si muove orizzontalmente avanti e indietro (ping-pong) attorno a un punto centrale.
    /// </summary>
    protected override void UpdateBehavior()
    {
        // Pattern movimento: ping-pong orizzontale
        float xOffset = Mathf.Sin(Time.time * moveSpeed * 0.5f) * patrolWidth;
        transform.position = new Vector3(patrolCenter.x + xOffset, patrolCenter.y, patrolCenter.z);

        // Pattern attacco: spara verso il basso ogni N secondi
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            Shoot();
            shootTimer = 0f;
        }
    }

    void Shoot()
    {
        if (enemyBulletPrefab == null) return;

        // Spawna proiettile sotto il boss
        Vector3 spawnPos = transform.position + Vector3.down * 0.5f;

        // usa la rotazione del prefab per orientare correttamente il proiettile
        GameObject bullet = Instantiate(enemyBulletPrefab, spawnPos, enemyBulletPrefab.transform.rotation);

        // Suono (opzionale)
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShoot();
    }

    protected override void OnDamageFeedback()
    {
        // Flash bianco veloce (opzionale)
        //StartCoroutine(FlashWhite());

        // Spawna esplosione davanti al boss (z negativo = più avanti)
        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -1f);
        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.SpawnSmall(explosionPos, 1f);
        }
    }

    System.Collections.IEnumerator FlashWhite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogWarning("[FLASH] NO SpriteRenderer found!");
            yield break;
        }

        Color originalColor = sr.color;

        // Flash bianco
        sr.color = Color.cyan;
        yield return new WaitForSeconds(0.15f);

        // Ripristina il colore
        sr.color = originalColor;

    }
}