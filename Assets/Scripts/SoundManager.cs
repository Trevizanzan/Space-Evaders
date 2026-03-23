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
        PlayOneShot(gameOverClip, 1.0f);
    }

    private void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}