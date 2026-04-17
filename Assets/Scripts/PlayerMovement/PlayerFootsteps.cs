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

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public void PlayFootstep()
    {
        float currentSpeed = (animator != null) ? animator.velocity.magnitude : 0f;
        if (currentSpeed < movementThreshold) return;

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
        int mapX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainData.size.x) * (terrainData.alphamapWidth - 1));
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainData.size.z) * (terrainData.alphamapHeight - 1));

        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
        float maxWeight = 0f;
        int textureIndex = 0;

        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
        {
            if (splatmapData[0, 0, i] > maxWeight)
            {
                maxWeight = splatmapData[0, 0, i];
                textureIndex = i;
            }
        }
        return terrainData.terrainLayers[textureIndex].name.ToLower();
    }
}
