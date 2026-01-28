using UnityEngine;

public class NoteAudioGenerator : MonoBehaviour
{
    [Header("Generated Note Sounds")]
    [SerializeField] private AudioSource audioSource;

    // Piano note frequencies (C4 to G4 for 5 fingers)
    private float[] noteFrequencies = {
        261.63f,  // C4 - Thumb
        293.66f,  // D4 - Index
        329.63f,  // E4 - Middle
        349.23f,  // F4 - Ring
        392.00f   // G4 - Pinky
    };

    private AudioClip[] generatedClips;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        GenerateNoteClips();
    }

    private void GenerateNoteClips()
    {
        generatedClips = new AudioClip[5];

        for (int i = 0; i < 5; i++)
        {
            generatedClips[i] = CreateToneClip(noteFrequencies[i], 0.3f, $"Note_{i}");
        }
    }

    private AudioClip CreateToneClip(float frequency, float duration, string name)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 5f); // Decay envelope

            // Piano-like tone (fundamental + harmonics)
            float sample = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.5f;
            sample += Mathf.Sin(4 * Mathf.PI * frequency * t) * 0.25f; // 2nd harmonic
            sample += Mathf.Sin(6 * Mathf.PI * frequency * t) * 0.125f; // 3rd harmonic

            samples[i] = sample * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void PlayNote(int fingerIndex)
    {
        if (fingerIndex >= 0 && fingerIndex < generatedClips.Length && generatedClips[fingerIndex] != null)
        {
            audioSource.PlayOneShot(generatedClips[fingerIndex], 0.7f);
        }
    }

    public AudioClip GetNoteClip(int fingerIndex)
    {
        if (fingerIndex >= 0 && fingerIndex < generatedClips.Length)
        {
            return generatedClips[fingerIndex];
        }
        return null;
    }

    public AudioClip[] GetAllClips()
    {
        return generatedClips;
    }
}
