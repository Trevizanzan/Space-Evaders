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
    public AudioClip playerHitClip;
    public AudioClip gameOverClip;
    public AudioClip bossEntranceClip;
    public AudioClip bossDeadClip;


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

    public void PlayShoot()
    {
        PlayOneShot(shootClip, 0.8f);
    }

    public void PlayAsteroidExplode()
    {
        PlayOneShot(asteroidExplodeClip, 0.7f);
    }

    public void PlayPlayerHit()
    {
        PlayOneShot(playerHitClip, 1.0f);
    }

    public void PlayGameOver()
    {
        PlayOneShot(gameOverClip, .7f);
    }

    #region Boss

    public void PlayBossEntrance(float duration = 0f)
    {
        PlayOneShot(bossEntranceClip, 1f, duration);
    }

    public void PlayBossDead()
    {
        PlayOneShot(bossDeadClip, 1f);
    }

    public void PlayBossHit()
    {
        PlayOneShot(playerHitClip, .25f);
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