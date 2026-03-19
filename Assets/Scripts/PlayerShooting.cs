using UnityEngine;

/// <summary>
/// questa classe si occupa solamente di gestire lo sparo del proiettile quando il giocatore preme la barra spaziatrice. (QUANDO e DOVE sparare)
/// non si occupa della logica del proiettile, che è gestita da un altro script attaccato al prefab del proiettile stesso.  (COME si comporta il proiettile)
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;  // slot per il prefab
    public Transform firePoint;      // punto dove spawna (davanti alla nave)

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        }
    }
}
