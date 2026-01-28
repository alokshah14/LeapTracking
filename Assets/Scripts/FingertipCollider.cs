using UnityEngine;
using Leap;

public class FingertipCollider : MonoBehaviour
{
    // These public fields will be set by the script that creates the colliders.
    // Finger indices: 0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky
    public Chirality Handedness;
    public int FingerIndex;
}
