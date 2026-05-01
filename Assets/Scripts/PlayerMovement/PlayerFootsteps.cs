using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("FMOD")]
    [SerializeField] private EventReference footstepEvent;
    [SerializeField] private string parameterName = "footsteps";
    [SerializeField] private string defaultLayer = "Unknown"; // Zmieniłem na Unknown pod Twój błąd

    [Header("Detekcja")]
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Animacja")]
    [SerializeField] private int actionLayerIndex = 2; // Wyciągnięty Magic Number

    private Animator _animator;
    private CharacterController _controller;

    void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        // OPTYMALIZACJA: Pobieramy to raz na starcie
        _controller = GetComponentInParent<CharacterController>();
    }

    public void PlayFootstep()
    {
        float currentSpeed = (_animator != null) ? _animator.velocity.magnitude : 0f;
        if (currentSpeed < movementThreshold) 
        {
            Debug.Log($"<color=orange>[Footsteps]</color> Zablokowane: Prędkość {currentSpeed} jest mniejsza niż próg {movementThreshold}");
            return;
        }

        if (_animator != null && _animator.layerCount > actionLayerIndex)
        {
            AnimatorStateInfo actionState = _animator.GetCurrentAnimatorStateInfo(actionLayerIndex);
            int nothingHash = Animator.StringToHash("Nothing");

            if (actionState.shortNameHash != nothingHash || _animator.IsInTransition(actionLayerIndex))
            {
                Debug.Log($"<color=orange>[Footsteps]</color> Zablokowane: Postać robi akcję (atak/unik) na warstwie {actionLayerIndex}");
                return;
            }
        }

        Sojartsa.Systems.Surface.SurfaceType currentSurface = Sojartsa.Systems.Surface.SurfaceType.Default;
        string materialLabel = DetermineMaterial(out currentSurface);

        EventInstance instance = RuntimeManager.CreateInstance(footstepEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        instance.setParameterByNameWithLabel(parameterName, materialLabel);

        // Zostawiam Pitch do decyzji Sound Designera w FMOD Studio
        instance.start();
        instance.release();

        // Odpalenie cząsteczek spod butów, jeśli są przypisane w SurfaceData
        if (Sojartsa.Systems.Surface.SurfaceManager.Instance != null && currentSurface != Sojartsa.Systems.Surface.SurfaceType.Default)
        {
            GameObject footstepVFX = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetFootstepVFX(currentSurface);
            if (footstepVFX != null)
            {
                // Odpalamy VFX na ziemi z rotacją do góry
                GameObject vfx = SimplePool.Spawn(footstepVFX, transform.position, Quaternion.LookRotation(Vector3.up));
                SimplePool.Despawn(vfx, footstepVFX, 2.0f);
            }
        }
    }

    private string DetermineMaterial(out Sojartsa.Systems.Surface.SurfaceType outSurfaceType)
    {
        outSurfaceType = Sojartsa.Systems.Surface.SurfaceType.Default;

        if (Sojartsa.Systems.Surface.SurfaceManager.Instance != null)
        {
            // Pytamy jedyny słuszny system o typ podłoża
            outSurfaceType = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetSurface(transform.position);
            
            // Rysujemy debugowy promień (przejęty ze skasowanego SurfaceTestera)
            Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 1f, Color.red, 1f);
            
            if (outSurfaceType == Sojartsa.Systems.Surface.SurfaceType.Default)
            {
                return defaultLayer;
            }
            
            // FMOD dostaje stringa z Enuma ("Grass", "Stone", "Wood" itd.)
            // Jeśli FMOD używa małych liter, dopisz tutaj: .ToLower()
            return outSurfaceType.ToString();
        }

        return defaultLayer;
    }
}