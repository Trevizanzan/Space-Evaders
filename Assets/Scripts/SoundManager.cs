using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip shootClip;
    public AudioClip asteroidExplodeClip;

    public AudioClip enemyShootClip;
    public AudioClip enemyDeadClip;

    public AudioClip bossEntranceClip;
    public AudioClip bossDeadClip;

    public AudioClip playerHitClip;
    public AudioClip gameOverClip;

    private float lastShootTime = -999f;
    [SerializeField] private float shootSoundCooldown = 0.65f; // max ~12 suoni/sec

    private float lastAsteroidExplodeTime = -999f;
    [SerializeField] private float asteroidExplodeSoundCooldown = 0.25f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Player

    public void PlayShoot()
    {
        //PlayOneShot(shootClip, 0.8f);

        ////if (Time.time - lastShootTime < shootSoundCooldown) return;
        ////lastShootTime = Time.time;

        ////// Pitch random per varietà
        ////sfxSource.pitch = Random.Range(0.3f, 0.5f);
        ////PlayOneShot(shootClip, 0.40f); // volume abbassato da 0.8 a 0.3
        ////sfxSource.pitch = 1f; // resetta dopo
    }

    public void PlayPlayerHit()
    {
        PlayOneShot(playerHitClip, .8f);
    }

    public void PlayGameOver()
    {
        PlayOneShot(gameOverClip, 1f);
    }

    #endregion

    #region Asteroid

    public void PlayAsteroidExplode()
    {
        //PlayOneShot(asteroidExplodeClip, 0.5f);

        if (Time.time - lastAsteroidExplodeTime < asteroidExplodeSoundCooldown) return;
        lastAsteroidExplodeTime = Time.time;
        PlayOneShot(asteroidExplodeClip, 0.4f);
    }

    #endregion

    #region Enemy

    public void PlayEnemyShoot()
    {
        // I nemici usano questo — nessun cooldown, ma volume più basso
        AudioClip clip = enemyShootClip != null ? enemyShootClip : shootClip;
        PlayOneShot(clip, 0.4f);
    }

    public void PlayEnemyDead()
    {
        PlayOneShot(enemyDeadClip, .5f);
    }

    #endregion

    #region Boss

    public void PlayBossEntrance(float duration = 0f)
    {
        PlayOneShot(bossEntranceClip, .8f, duration);
    }

    public void PlayBossDead()
    {
        PlayOneShot(bossDeadClip, 1f);
    }

    public void PlayBossHit()
    {
        //PlayOneShot(playerHitClip, .4f);
    }

    #endregion

    private void PlayOneShot(AudioClip clip, float volume = 1f, float duration = 0f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);

        if (duration > 0)
        {
            StartCoroutine(StopSoundAfterDuration(duration));
        }
    }

    private IEnumerator StopSoundAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        sfxSource.Stop();
    }
}