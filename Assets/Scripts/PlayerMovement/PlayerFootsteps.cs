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
    [SerializeField] private float rayDistance = 1.5f;
    [SerializeField] private float rayHeightOffset = 0.5f; // Punkt startowy nad stopami
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Animacja")]
    [SerializeField] private int actionLayerIndex = 2; // Wyciągnięty Magic Number

    private string[] _cachedTerrainLayerNames;
    private Terrain _currentTerrain; // Pamięta teren, na którym stoimy

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
        if (currentSpeed < movementThreshold) return;

        // FROMSOFTWARE FIX: Z użyciem zcache'owanego kontrolera (nie obciąża procesora)
       // if (_controller != null && !_controller.isGrounded) return;

        if (_animator != null && _animator.layerCount > actionLayerIndex)
        {
            AnimatorStateInfo actionState = _animator.GetCurrentAnimatorStateInfo(actionLayerIndex);
            int nothingHash = Animator.StringToHash("Nothing");

            if (actionState.shortNameHash != nothingHash || _animator.IsInTransition(actionLayerIndex))
            {
                return;
            }
        }

        string materialLabel = DetermineMaterial();

        EventInstance instance = RuntimeManager.CreateInstance(footstepEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        instance.setParameterByNameWithLabel(parameterName, materialLabel);

        // Zostawiam Pitch do decyzji Sound Designera w FMOD Studio
        instance.start();
        instance.release();

        Debug.Log($"<color=lime>Krok!</color> Materiał: {materialLabel}");
    }

    private string DetermineMaterial()
    {
        // FIX: Strzelamy centralnie w dół ze środka postaci (lekko uniesione żeby nie wejść w podłogę)
        // Usunąłem transform.forward, żeby moonwalk i strafe działały poprawnie
        Vector3 rayStart = transform.position + (Vector3.up * rayHeightOffset);

        // DEBUG: Narysuje Ci czerwoną linię w oknie Scene podczas grania!
        Debug.DrawRay(rayStart, Vector3.down * rayDistance, Color.red, 1f);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, floorLayer))
        {
            if (hit.collider is TerrainCollider terrainCollider)
            {
                Terrain hitTerrain = terrainCollider.GetComponent<Terrain>();
                return GetTerrainTexture(terrainCollider.terrainData, hit.point, terrainCollider.transform.position, hitTerrain);
            }

            // Możesz tu dodawać kolejne ify dla innych modeli, np. jeśli mają tag "Wood" itp.
            return defaultLayer;
        }

        // Raycast nic nie trafił
        return defaultLayer;
    }

    private string GetTerrainTexture(TerrainData terrainData, Vector3 worldPos, Vector3 terrainPos, Terrain hitTerrain)
    {
        // FIX: Dynamiczny cache. Jeśli weszliśmy na nowy kawałek terenu, to robi cache tylko raz.
        if (_currentTerrain != hitTerrain)
        {
            _currentTerrain = hitTerrain;
            CacheTerrainLayers(_currentTerrain);
        }

        if (_cachedTerrainLayerNames == null || _cachedTerrainLayerNames.Length == 0) return defaultLayer;

        int mapX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainData.size.x) * (terrainData.alphamapWidth - 1));
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainData.size.z) * (terrainData.alphamapHeight - 1));

        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        float maxWeight = 0f;
        int textureIndex = 0;

        for (int i = 0; i < _cachedTerrainLayerNames.Length; i++)
        {
            if (splatmapData[0, 0, i] > maxWeight)
            {
                maxWeight = splatmapData[0, 0, i];
                textureIndex = i;
            }
        }

        return _cachedTerrainLayerNames[textureIndex];
    }

    private void CacheTerrainLayers(Terrain terrain)
    {
        if (terrain != null && terrain.terrainData != null)
        {
            var layers = terrain.terrainData.terrainLayers;
            _cachedTerrainLayerNames = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                _cachedTerrainLayerNames[i] = layers[i].name.ToLower();
            }
        }
    }
}