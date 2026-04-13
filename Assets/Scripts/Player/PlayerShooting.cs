using UnityEngine;

/// <summary>
/// questa classe si occupa solamente di gestire lo sparo del proiettile quando il giocatore preme la barra spaziatrice. (QUANDO e DOVE sparare)
/// non si occupa della logica del proiettile, che è gestita da un altro script attaccato al prefab del proiettile stesso.  (COME si comporta il proiettile)
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;  // slot per il prefab
    public Transform firePoint;      // punto dove spawna (davanti alla nave)

    // Aggiunta cooldown per evitare di sparare troppo velocemente
    private float lastShootTime = 0f;

    [Header("Auto Fire")]
    [SerializeField] private bool autoFire = false;

    [SerializeField] private float shootCooldown = 0.13f; // tra uno sparo e l'altro devono passare almeno 0.13 secondi (8 spari al secondo)

    void Update()
    {
        // Il player non può sparare se il boss sta entrando
        if (BossBase.IsBossEntering) return;

        // tasto destro del mouse o barra spaziatrice per sparare
        bool manualFire = (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(1))
                          && Time.time - lastShootTime >= shootCooldown;

        bool shouldShoot = autoFire
            ? Time.time - lastShootTime >= shootCooldown
            : manualFire;

        if (shouldShoot)
        {
            Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayShoot();

            lastShootTime = Time.time;
        }
    }
}
