using UnityEngine;

[DisallowMultipleComponent]
public sealed class BackgroundMusic : MonoBehaviour
{
    private const string DefaultResourcesPath = "Audio/background_music";
    private static BackgroundMusic instance;

    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool persistBetweenScenes = true;

    private AudioSource audioSource;

    private void Awake()
    {
        if (persistBetweenScenes && instance != null && instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (audioSource.clip == null)
        {
            Debug.LogWarning("BackgroundMusic has no AudioClip assigned.", this);
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    public static void PlayCurrent()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<BackgroundMusic>();
        }

        if (instance != null)
        {
            instance.Play();
        }
    }

    public static void StopCurrent()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<BackgroundMusic>();
        }

        if (instance != null)
        {
            instance.Stop();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void ConfigureAudioSource()
    {
        AudioClip clip = musicClip != null ? musicClip : Resources.Load<AudioClip>(DefaultResourcesPath);

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }
}
