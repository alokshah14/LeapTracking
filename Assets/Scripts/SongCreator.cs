using UnityEngine;
using System.Collections.Generic;
using Leap;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SongCreator : MonoBehaviour
{
    [Header("Create songs at runtime for testing")]
    [SerializeField] private RhythmGameManager rhythmManager;

    // Creates songs programmatically - call from inspector button or at start
    public static Song CreateScaleUp(string name = "Scale Up")
    {
        Song song = ScriptableObject.CreateInstance<Song>();
        song.songName = name;
        song.bpm = 90f;
        song.speedMultiplier = 1f;
        song.notes = new List<Song.NoteData>();

        // Right hand ascending scale: Thumb -> Pinky
        float beat = 0f;
        for (int i = 0; i < 5; i++)
        {
            song.notes.Add(new Song.NoteData
            {
                hand = Chirality.Right,
                fingerIndex = i,
                beatTime = beat,
                duration = 0.8f
            });
            beat += 1f;
        }

        // Descending: Pinky -> Thumb
        for (int i = 4; i >= 0; i--)
        {
            song.notes.Add(new Song.NoteData
            {
                hand = Chirality.Right,
                fingerIndex = i,
                beatTime = beat,
                duration = 0.8f
            });
            beat += 1f;
        }

        return song;
    }

    public static Song CreateFingerExercise(string name = "Finger Exercise")
    {
        Song song = ScriptableObject.CreateInstance<Song>();
        song.songName = name;
        song.bpm = 100f;
        song.speedMultiplier = 1f;
        song.notes = new List<Song.NoteData>();

        float beat = 0f;

        // Pattern: Index-Middle-Ring-Middle (common piano exercise)
        int[] pattern = { 1, 2, 3, 2, 1, 2, 3, 4, 3, 2, 1, 0 };

        foreach (int finger in pattern)
        {
            song.notes.Add(new Song.NoteData
            {
                hand = Chirality.Right,
                fingerIndex = finger,
                beatTime = beat,
                duration = 0.5f
            });
            beat += 0.75f;
        }

        return song;
    }

    public static Song CreateTwinkleTwinkle(string name = "Twinkle Twinkle")
    {
        Song song = ScriptableObject.CreateInstance<Song>();
        song.songName = name;
        song.bpm = 80f;
        song.speedMultiplier = 1f;
        song.notes = new List<Song.NoteData>();

        // Simplified Twinkle Twinkle using 5 fingers
        // Original: C C G G A A G | F F E E D D C
        // Mapped to fingers: 0=C, 1=D, 2=E, 3=F, 4=G (simplified)
        // Thumb(0), Index(1), Middle(2), Ring(3), Pinky(4)

        // "Twin-kle twin-kle lit-tle star"
        int[] melody = { 0, 0, 4, 4, 4, 4, 4 }; // C C G G A A G (simplified)
        float[] durations = { 1, 1, 1, 1, 1, 1, 2 };

        float beat = 0f;
        for (int i = 0; i < melody.Length; i++)
        {
            song.notes.Add(new Song.NoteData
            {
                hand = Chirality.Right,
                fingerIndex = Mathf.Clamp(melody[i], 0, 4),
                beatTime = beat,
                duration = durations[i] * 0.8f
            });
            beat += durations[i];
        }

        // "How I won-der what you are"
        int[] melody2 = { 3, 3, 2, 2, 1, 1, 0 };
        float[] durations2 = { 1, 1, 1, 1, 1, 1, 2 };

        for (int i = 0; i < melody2.Length; i++)
        {
            song.notes.Add(new Song.NoteData
            {
                hand = Chirality.Right,
                fingerIndex = melody2[i],
                beatTime = beat,
                duration = durations2[i] * 0.8f
            });
            beat += durations2[i];
        }

        return song;
    }

    public static Song CreateSpeedChallenge(string name = "Speed Challenge")
    {
        Song song = ScriptableObject.CreateInstance<Song>();
        song.songName = name;
        song.bpm = 120f;
        song.speedMultiplier = 1f;
        song.notes = new List<Song.NoteData>();

        float beat = 0f;

        // Starts slow, gets faster
        float[] gaps = { 1f, 1f, 0.75f, 0.75f, 0.5f, 0.5f, 0.5f, 0.5f, 0.25f, 0.25f, 0.25f, 0.25f };
        int[] fingers = { 1, 2, 1, 3, 1, 2, 3, 4, 1, 2, 3, 4 };

        for (int i = 0; i < fingers.Length; i++)
        {
            song.notes.Add(new Song.NoteData
            {
                hand = Chirality.Right,
                fingerIndex = fingers[i],
                beatTime = beat,
                duration = gaps[i] * 0.8f
            });
            beat += gaps[i];
        }

        return song;
    }

    public static Song CreateRandomPractice(string name = "Random Practice", int noteCount = 20, float bpm = 100f)
    {
        Song song = ScriptableObject.CreateInstance<Song>();
        song.songName = name;
        song.bpm = bpm;
        song.speedMultiplier = 1f;
        song.notes = new List<Song.NoteData>();

        float beat = 0f;

        for (int i = 0; i < noteCount; i++)
        {
            song.notes.Add(new Song.NoteData
            {
                hand = Chirality.Right,
                fingerIndex = Random.Range(0, 5),
                beatTime = beat,
                duration = 0.5f
            });
            beat += Random.Range(0.5f, 1.5f);
        }

        return song;
    }

#if UNITY_EDITOR
    [MenuItem("Finger Piano/Create Default Songs")]
    public static void CreateDefaultSongs()
    {
        string path = "Assets/Songs";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "Songs");
        }

        // Create and save songs
        SaveSong(CreateScaleUp(), path + "/ScaleUp.asset");
        SaveSong(CreateFingerExercise(), path + "/FingerExercise.asset");
        SaveSong(CreateTwinkleTwinkle(), path + "/TwinkleTwinkle.asset");
        SaveSong(CreateSpeedChallenge(), path + "/SpeedChallenge.asset");
        SaveSong(CreateRandomPractice("Random Easy", 15, 80f), path + "/RandomEasy.asset");
        SaveSong(CreateRandomPractice("Random Hard", 30, 140f), path + "/RandomHard.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created default songs in Assets/Songs folder!");
    }

    private static void SaveSong(Song song, string path)
    {
        AssetDatabase.CreateAsset(song, path);
        Debug.Log($"Created song: {song.songName}");
    }
#endif
}
