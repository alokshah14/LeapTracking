using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Leap;
using System;
using System.Text;

public class HandDataLogger : MonoBehaviour
{
    public static HandDataLogger Instance { get; private set; }

    [Header("Logging Settings")]
    [Tooltip("Enable this to start logging automatically when the game starts.")]
    public bool logOnStart = true;
    [Tooltip("The base filename for the log file.")]
    public string logFileName = "HandDataLog";

    private StreamWriter writer;
    private bool isLogging = false;
    private StringBuilder stringBuilder = new StringBuilder();

    private string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
    private string[] boneNames = { "Metacarpal", "Proximal", "Intermediate", "Distal" };

    // For calculating tip velocities (since TipVelocity was removed from newer SDK)
    private Dictionary<int, Vector3[]> previousTipPositions = new Dictionary<int, Vector3[]>();
    private float previousTime;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep the logger active across scenes
    }

    void Start()
    {
        if (logOnStart)
        {
            StartLogging();
        }
    }

    public void StartLogging()
    {
        if (isLogging)
        {
            Debug.LogWarning("HandDataLogger is already logging.");
            return;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, "HandData");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fullPath = Path.Combine(folderPath, $"{logFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        writer = new StreamWriter(fullPath, false, Encoding.UTF8);
        isLogging = true;

        WriteHeader();
        Debug.Log($"HandDataLogger started. Logging to: {fullPath}");
    }

    private void WriteHeader()
    {
        stringBuilder.Clear();
        stringBuilder.Append("Timestamp,Frame ID,EventType,EventDetails,");
        stringBuilder.Append("Hand,Hand ID,");

        for (int i = 0; i < 5; i++) // For each finger
        {
            string fingerPrefix = fingerNames[i];
            stringBuilder.Append($"{fingerPrefix}_Tip_Vel_X,{fingerPrefix}_Tip_Vel_Y,{fingerPrefix}_Tip_Vel_Z,");

            for (int j = 0; j < 4; j++) // For each bone
            {
                string bonePrefix = $"{fingerPrefix}_{boneNames[j]}";
                stringBuilder.Append($"{bonePrefix}_Pos_X,{bonePrefix}_Pos_Y,{bonePrefix}_Pos_Z,");
                stringBuilder.Append($"{bonePrefix}_Rot_W,{bonePrefix}_Rot_X,{bonePrefix}_Rot_Y,{bonePrefix}_Rot_Z,");
                if (j > 0) // Joint angle makes sense for non-metacarpal bones
                {
                    stringBuilder.Append($"{fingerPrefix}_Joint_{j}_Angle,");
                }
            }
        }
        writer.WriteLine(stringBuilder.ToString().TrimEnd(','));
    }

    public void StopLogging()
    {
        if (!isLogging) return;

        isLogging = false;
        if (writer != null)
        {
            writer.Close();
            writer = null;
            Debug.Log("HandDataLogger stopped.");
        }
    }

    public void LogGameEvent(string eventType, string eventDetails)
    {
        if (!isLogging) return;
        
        stringBuilder.Clear();
        stringBuilder.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},{Time.frameCount},{eventType},{eventDetails},");
        // Fill the rest of the line with empty commas to maintain column alignment
        stringBuilder.Append(new string(',', 481)); // 481 is the number of data columns, adjust if header changes
        writer.WriteLine(stringBuilder.ToString().TrimEnd(','));
    }

    public void LogHandFrame(Frame frame)
    {
        if (!isLogging || frame == null) return;

        float currentTime = Time.time;
        float deltaTime = currentTime - previousTime;

        foreach (Hand hand in frame.Hands)
        {
            stringBuilder.Clear();
            stringBuilder.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},{frame.Id},HandData,,");
            stringBuilder.Append($"{(hand.IsLeft ? "Left" : "Right")},{hand.Id},");

            // Get or create previous positions array for this hand
            if (!previousTipPositions.TryGetValue(hand.Id, out Vector3[] prevPositions))
            {
                prevPositions = new Vector3[5];
                previousTipPositions[hand.Id] = prevPositions;
            }

            Vector3[] currentPositions = new Vector3[5];

            // Access fingers directly via properties (newer Ultraleap SDK)
            Finger[] fingers = { hand.Thumb, hand.Index, hand.Middle, hand.Ring, hand.Pinky };

            for (int i = 0; i < fingers.Length; i++)
            {
                Finger finger = fingers[i];
                Vector3 tipPos = finger.TipPosition;
                currentPositions[i] = tipPos;

                // Calculate tip velocity
                Vector3 tipVelocity = Vector3.zero;
                if (deltaTime > 0 && prevPositions[i] != Vector3.zero)
                {
                    tipVelocity = (tipPos - prevPositions[i]) / deltaTime;
                }
                stringBuilder.Append($"{tipVelocity.x},{tipVelocity.y},{tipVelocity.z},");

                // Bone Data (using bones array - newer Ultraleap SDK)
                for (int j = 0; j < 4; j++)
                {
                    Bone bone = finger.bones[j];

                    stringBuilder.Append($"{bone.Center.x},{bone.Center.y},{bone.Center.z},");
                    stringBuilder.Append($"{bone.Rotation.w},{bone.Rotation.x},{bone.Rotation.y},{bone.Rotation.z},");

                    // Joint Angle
                    if (j > 0)
                    {
                        Bone prevBone = finger.bones[j - 1];
                        float angle = Vector3.Angle(prevBone.Direction, bone.Direction);
                        stringBuilder.Append(angle + ",");
                    }
                }
            }

            // Store current positions for next frame's velocity calculation
            previousTipPositions[hand.Id] = currentPositions;

            writer.WriteLine(stringBuilder.ToString().TrimEnd(','));
        }

        previousTime = currentTime;
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }
}
