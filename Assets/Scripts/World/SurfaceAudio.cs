using UnityEngine;

/// <summary>
/// Prosty komponent do oznaczania powierzchni dla dźwięków FMOD.
/// Przykład: "Flesh", "Stone", "Wood", "Metal".
/// </summary>
public class SurfaceAudio : MonoBehaviour
{
    [Tooltip("Etykieta materiału w FMOD (Label)")]
    public string surfaceLabel = "Flesh";
}
