using UnityEngine;
using FMODUnity;
using FMOD.Studio;


public class AudioInteractableObjects : MonoBehaviour
{
    [Header("FMOD Ustawienia")]
    [SerializeField] private EventReference audioEvent;

    [Tooltip("Nazwa parametru w FMOD (np. Chest)")]
    [SerializeField] private string fmodParameterName = "Chest";

    private FMOD.Studio.EventInstance _audioInstance;

    private void Start()
    {
        if (!audioEvent.IsNull)
        {
            _audioInstance = RuntimeManager.CreateInstance(audioEvent);
            RuntimeManager.AttachInstanceToGameObject(_audioInstance, transform);
            // USUNIÊTO: _audioInstance.start(); <- To tutaj powodowa³o problem!
        }
    }

    private void OnDestroy()
    {
        _audioInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        _audioInstance.release();
    }

    public void PlaySoundWithParameter(string parameterValue)
    {
        if (audioEvent.IsNull) return;

        _audioInstance.setParameterByNameWithLabel(fmodParameterName, parameterValue);
        _audioInstance.start(); // Teraz FMOD zagra dopiero, gdy dostanie sygna³ z UnityEvent!
    }
}