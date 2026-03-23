using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    // Singleton - by kazdy skrypt mogl go wywolac bez szukania
    public static HitStop Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Zamraza gre na chwile. duration = ile sekund (0.05 = subtelne, 0.1 = mocne)
    /// </summary>
    public void Freeze(float duration)
    {
        StartCoroutine(DoFreeze(duration));
    }

private IEnumerator DoFreeze(float duration)
{
    float originalDelta = 0.02f; // Standardowe 50Hz fizyki
    
    Time.timeScale = 0.05f; // Nie 0.02, daj 0.05 - postać prawie stoi, ale silnik "żyje"
    Time.fixedDeltaTime = Time.timeScale * originalDelta; 

    // Czekamy w czasie rzeczywistym (bo timeScale jest bliski zeru!)
    yield return new WaitForSecondsRealtime(duration);

    Time.timeScale = 1f;
    Time.fixedDeltaTime = originalDelta;
}
}
