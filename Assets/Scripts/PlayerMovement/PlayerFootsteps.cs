using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("FMOD")]
    [SerializeField] private EventReference footstepEvent;
    [SerializeField] private string parameterName = "footsteps";
    [SerializeField] private string defaultLayer = "dirt";

    [Header("Detekcja")]
    [SerializeField] private float rayDistance = 1.5f;
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private float movementThreshold = 0.1f;
    
    private string[] _cachedTerrainLayerNames;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // OPTYMALIZACJA: Cache'ujemy nazwy tekstur na starcie, żeby nie robić tego co krok
        CacheTerrainLayers();
    }

    private void CacheTerrainLayers()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null && terrain.terrainData != null)
        {
            var layers = terrain.terrainData.terrainLayers;
            _cachedTerrainLayerNames = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                _cachedTerrainLayerNames[i] = layers[i].name.ToLower();
            }
            Debug.Log($"[Footsteps] Zcache'owano {layers.Length} warstw terenu.");
        }
    }

    public void PlayFootstep()
    {
        float currentSpeed = (animator != null) ? animator.velocity.magnitude : 0f;
        if (currentSpeed < movementThreshold) return;

        // --- FROMSOFTWARE FIX: Blokada w powietrzu i w trakcie walki/uników ---
        CharacterController controller = GetComponentInParent<CharacterController>();
        if (controller != null && !controller.isGrounded) return; // Zabezpieczenie przed chodzeniem w powietrzu

        if (animator != null && animator.layerCount > 2)
        {
            // Sprawdzamy warstwę ACTIONS (indeks 2) - tak samo jak w PlayerMovement i PlayerCombat
            AnimatorStateInfo actionState = animator.GetCurrentAnimatorStateInfo(2);
            int nothingHash = Animator.StringToHash("Nothing");
            
            // Jeśli gramy animację na warstwie ataku/uniku lub jesteśmy w przejściu
            if (actionState.shortNameHash != nothingHash || animator.IsInTransition(2))
            {
                return; // Gracz atakuje albo robi unik, wycisz kroki!
            }
        }
        // ----------------------------------------------------------------------

        string materialLabel = DetermineMaterial();

        // System One-Shot - naprawia opóźnienia i kolejkowanie
        EventInstance instance = RuntimeManager.CreateInstance(footstepEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        instance.setParameterByNameWithLabel(parameterName, materialLabel);
        instance.setPitch(1.5f);
        instance.start();
        instance.release();

        Debug.Log($"<color=lime>Krok!</color> Materiał: {materialLabel}");
    }

    private string DetermineMaterial()
    {
        // Strzelamy Raycastem lekko z przodu (0.2f), żeby zniwelować lag Root Motion
        Vector3 rayStart = transform.position + (transform.forward * 0.2f) + (Vector3.up * 0.2f);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, floorLayer))
        {
            // Sprawdzamy tylko teren
            if (hit.collider is TerrainCollider terrainCollider)
            {
                return GetTerrainTexture(terrainCollider.terrainData, hit.point, terrainCollider.transform.position);
            }

            // Jeśli to nie teren (np. model mostu), możesz tu opcjonalnie 
            // sprawdzać nazwę obiektu, ale na razie zwracamy default:
            return defaultLayer;
        }
        return defaultLayer;
    }

    private string GetTerrainTexture(TerrainData terrainData, Vector3 worldPos, Vector3 terrainPos)
    {
        if (_cachedTerrainLayerNames == null || _cachedTerrainLayerNames.Length == 0) return defaultLayer;

        int mapX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainData.size.x) * (terrainData.alphamapWidth - 1));
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainData.size.z) * (terrainData.alphamapHeight - 1));

        // TO JEST CIĘŻKIE API, ale pobieramy tylko 1x1 piksel
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
        
        // ZWRACAMY ZCACHE'OWANY STRING (brak nowych alokacji pamięci!)
        return _cachedTerrainLayerNames[textureIndex];
    }
}
