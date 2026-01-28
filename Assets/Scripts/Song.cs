using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSong", menuName = "Finger Piano/Song")]
public class Song : ScriptableObject
{
    [Header("Song Info")]
    public string songName = "Untitled";
    public string artist = "Unknown";
    public float bpm = 120f; // Beats per minute

    [Header("Audio")]
    [Tooltip("Background music track (optional)")]
    public AudioClip backgroundMusic;

    [Tooltip("Individual note sounds - assign 5 clips for each finger (Thumb to Pinky)")]
    public AudioClip[] noteSounds = new AudioClip[5];

    [Header("Difficulty")]
    [Range(0.5f, 2f)]
    public float speedMultiplier = 1f;

    [Header("Notes")]
    [Tooltip("The sequence of notes in this song")]
    public List<NoteData> notes = new List<NoteData>();

    [Serializable]
    public class NoteData
    {
        [Tooltip("Which hand plays this note")]
        public Leap.Chirality hand = Leap.Chirality.Right;

        [Tooltip("Which finger (0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky)")]
        [Range(0, 4)]
        public int fingerIndex = 1;

        [Tooltip("When this note plays (in beats from song start)")]
        public float beatTime = 0f;

        [Tooltip("How long the key stays highlighted (in beats)")]
        public float duration = 0.5f;
    }

    // Helper to get seconds from beats
    public float BeatsToSeconds(float beats)
    {
        return (beats / bpm) * 60f * (1f / speedMultiplier);
    }

    // Helper to get the song duration
    public float GetDurationInSeconds()
    {
        if (notes.Count == 0) return 0f;

        float lastBeat = 0f;
        foreach (var note in notes)
        {
            float noteEnd = note.beatTime + note.duration;
            if (noteEnd > lastBeat) lastBeat = noteEnd;
        }

        return BeatsToSeconds(lastBeat) + 2f; // Add 2 seconds buffer
    }
}
