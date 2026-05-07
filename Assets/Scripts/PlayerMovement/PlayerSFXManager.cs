using UnityEngine;
using FMODUnity;

/// <summary>
/// Centralny zarządca dźwięków gracza (SFX). 
/// Tu trzymaj wszystko co nie jest bezpośrednio związane z bronią (uniki, szuranie stopą, staminę itp.).
/// </summary>
public class PlayerSFXManager : MonoBehaviour
{
    public static PlayerSFXManager Instance { get; private set; }

    [Header("Dźwięki Akcji")]
    public EventReference dodgeSound;
    
    [Header("Dźwięki Ruchu (Zależne od podłoża)")]
    public EventReference scuffSound;
    public string surfaceParameterName = "Surface";

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void PlayDodge()
    {
        if (!dodgeSound.IsNull)
        {
            RuntimeManager.PlayOneShot(dodgeSound, transform.position);
        }
    }

    public void PlayScuff()
    {
        if (scuffSound.IsNull) return;

        // Sprawdzamy podłoże pod graczem
        string materialLabel = "Flesh"; 
        if (Sojartsa.Systems.Surface.SurfaceManager.Instance != null)
        {
            var surfaceType = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetSurface(transform.position);
            if (surfaceType != Sojartsa.Systems.Surface.SurfaceType.Default)
            {
                materialLabel = surfaceType.ToString();
            }
        }

        var instance = RuntimeManager.CreateInstance(scuffSound);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        instance.setParameterByNameWithLabel(surfaceParameterName, materialLabel);
        instance.start();
        instance.release();
    }
}
