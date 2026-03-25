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
    private float shootCooldown = 0.13f; // tra uno sparo e l'altro devono passare almeno 0.12 secondi (8 spari al secondo)
    private float lastShootTime = 0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastShootTime >= shootCooldown)
            {
                Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);

                // suona il suono dello sparo
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayShoot();
                }

                lastShootTime = Time.time;  // aggiorna il tempo dell'ultimo sparo
            }
        }
    }
}
