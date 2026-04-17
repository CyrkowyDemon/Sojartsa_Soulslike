using UnityEngine;
using UnityEngine.Events;

public class AnimationEventRelay : MonoBehaviour
{
    [Header("Co ma siê staæ po zakoñczeniu animacji?")]
    public UnityEvent OnAnimationFinished;

    // Tê funkcjê zobaczy okienko Animation!
    public void TriggerAnimationFinished()
    {
        OnAnimationFinished?.Invoke();
    }
}