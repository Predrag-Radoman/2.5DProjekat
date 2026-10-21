using UnityEngine;

/// <summary>
/// Proceduralni footstep/land zvuci: klipovi se generisu u runtime-u
/// (filtrirani sum sa decay envelope-om), bez ijednog audio fajla.
/// Menja se pravim SFX klipovima kad stignu (AudioManager pool).
/// </summary>
public class ProceduralAudio : MonoBehaviour
{
    [Header("Footstep")]
    [SerializeField] private float footstepLength = 0.09f;
    [SerializeField] private float footstepDecay = 42f;
    [SerializeField] private float footstepVolume = 0.35f;
    [SerializeField] private float footstepPitchVariation = 0.08f;

    [Header("Land")]
    [SerializeField] private float landLength = 0.2f;
    [SerializeField] private float landDecay = 16f;
    [SerializeField] private float landVolume = 0.5f;

    private AudioSource source;
    private AudioClip footstepL;
    private AudioClip footstepR;
    private AudioClip land;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0.6f;

        footstepL = MakeNoiseClip("FootstepL", footstepLength, footstepDecay, 0.55f);
        footstepR = MakeNoiseClip("FootstepR", footstepLength, footstepDecay * 0.92f, 0.5f);
        land = MakeNoiseClip("Land", landLength, landDecay, 0.28f);
    }

    public void PlayFootstep(bool left)
    {
        if (source == null) return;
        source.pitch = 1f + Random.Range(-footstepPitchVariation, footstepPitchVariation);
        source.volume = footstepVolume;
        source.PlayOneShot(left ? footstepL : footstepR);
    }

    public void PlayLand()
    {
        if (source == null) return;
        source.pitch = 1f;
        source.volume = landVolume;
        source.PlayOneShot(land);
    }

    private static AudioClip MakeNoiseClip(string name, float length, float decay, float smooth)
    {
        int samples = Mathf.Max(64, (int)(AudioSettings.outputSampleRate * length));
        float[] data = new float[samples];
        float prev = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)AudioSettings.outputSampleRate;
            float env = Mathf.Exp(-t * decay);
            float noise = Random.value * 2f - 1f;
            prev = Mathf.Lerp(noise, prev, smooth);
            data[i] = prev * env;
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, AudioSettings.outputSampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
