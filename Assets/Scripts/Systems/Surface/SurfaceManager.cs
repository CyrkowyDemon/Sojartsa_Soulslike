using UnityEngine;
using System.Collections.Generic;

namespace Sojartsa.Systems.Surface
{
    [DefaultExecutionOrder(-100)] 
    public class SurfaceManager : MonoBehaviour
    {
        public static SurfaceManager Instance { get; private set; }

        [Header("Konfiguracja")]
        [SerializeField] private SurfaceData surfaceData;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float raycastDistance = 2f;

        private Dictionary<PhysicsMaterial, SurfaceType> _physicDict = new Dictionary<PhysicsMaterial, SurfaceType>();
        private Dictionary<TerrainLayer, SurfaceType> _terrainDict = new Dictionary<TerrainLayer, SurfaceType>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Opcjonalnie: DontDestroyOnLoad(gameObject); // Jeśli chcesz go mieć między scenami
            }
            else
            {
                Destroy(gameObject);
            }

            InitializeDictionaries();
        }

        private void InitializeDictionaries()
        {
            if (surfaceData == null) return;

            _physicDict.Clear();
            foreach (var mapping in surfaceData.physicMaterialMappings)
            {
                if (mapping.material != null && !_physicDict.ContainsKey(mapping.material))
                    _physicDict.Add(mapping.material, mapping.type);
            }

            _terrainDict.Clear();
            foreach (var mapping in surfaceData.terrainLayerMappings)
            {
                if (mapping.layer != null && !_terrainDict.ContainsKey(mapping.layer))
                    _terrainDict.Add(mapping.layer, mapping.type);
            }
        }

        public SurfaceType GetSurface(RaycastHit hit)
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null) return GetTerrainSurface(terrain, hit.point);

            PhysicsMaterial mat = hit.collider.sharedMaterial;
            if (mat != null && _physicDict.TryGetValue(mat, out SurfaceType type)) return type;

            return SurfaceType.Default;
        }

        public SurfaceType GetSurface(Vector3 worldPos)
        {
            Ray ray = new Ray(worldPos + Vector3.up * 0.5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundMask))
            {
                return GetSurface(hit);
            }
            return SurfaceType.Default;
        }

        private SurfaceType GetTerrainSurface(Terrain terrain, Vector3 worldPos)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;

            int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
            int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

            mapX = Mathf.Clamp(mapX, 0, terrainData.alphamapWidth - 1);
            mapZ = Mathf.Clamp(mapZ, 0, terrainData.alphamapHeight - 1);

            float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            int strongestLayerIndex = 0;
            float maxOpacity = 0f;

            for (int i = 0; i < splatmapData.GetLength(2); i++)
            {
                if (splatmapData[0, 0, i] > maxOpacity)
                {
                    maxOpacity = splatmapData[0, 0, i];
                    strongestLayerIndex = i;
                }
            }

            if (strongestLayerIndex < terrainData.terrainLayers.Length)
            {
                TerrainLayer layer = terrainData.terrainLayers[strongestLayerIndex];
                if (layer != null && _terrainDict.TryGetValue(layer, out SurfaceType type)) return type;
            }

            return SurfaceType.Default;
        }
    }
}
