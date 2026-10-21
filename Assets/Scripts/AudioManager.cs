using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioManager skeleton: pool SFX izvora + muzika + event wiring na PlayerController.
/// Klipovi se popunjavaju u Inspector-u kad stignu zvuci; dok su slotovi prazni,
/// svi pozivi su bezopasni (null-check).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool SFX izvora")]
    [SerializeField] private int sfxPoolSize = 8;

    [Header("Glasnoća (0-1)")]
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float musicVolume = 1f;

    [Header("Klipovi (popuni kad stignu zvuci)")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    private readonly Stack<AudioSource> pool = new Stack<AudioSource>();
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < sfxPoolSize; i++) pool.Push(CreateSource("SFX_" + i));
        musicSource = CreateSource("Music");
        musicSource.loop = true;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.OnJumped += () => PlaySFX(jumpClip);
            pc.OnLanded += () => PlaySFX(landClip);
        }
    }

    private AudioSource CreateSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D zvuk za sada
        return src;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0.05f)
    {
        if (clip == null) return;
        AudioSource src = pool.Count > 0 ? pool.Pop() : CreateSource("SFX_extra");
        src.clip = clip;
        src.volume = volumeScale * sfxVolume;
        src.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
        src.Play();
        StartCoroutine(ReturnWhenDone(src));
    }

    private IEnumerator ReturnWhenDone(AudioSource src)
    {
        while (src.isPlaying) yield return null;
        src.clip = null;
        pool.Push(src);
    }

    /// <summary>Pozivati iz animation events-a FootstepL / FootstepR.</summary>
    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        PlaySFX(footstepClips[Random.Range(0, footstepClips.Length)]);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
