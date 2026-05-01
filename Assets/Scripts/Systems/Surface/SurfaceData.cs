using UnityEngine;
using System;
using System.Collections.Generic;

namespace Sojartsa.Systems.Surface
{
    public enum SurfaceType
    {
        Default,
        Grass,
        Stone,
        Wood,
        Mud,
        Metal,
        Water,
        Sand,
        Snow,
        Flesh // Dodane pod nowy system walki
    }

    [CreateAssetMenu(fileName = "NewSurfaceData", menuName = "Sojartsa/Surface System/Surface Data")]
    public class SurfaceData : ScriptableObject
    {
        [Header("Mapowanie Materiałów Fizycznych")]
        public List<PhysicMaterialMapping> physicMaterialMappings = new List<PhysicMaterialMapping>();

        [Header("Mapowanie Warstw Terenu")]
        public List<TerrainLayerMapping> terrainLayerMappings = new List<TerrainLayerMapping>();

        [Serializable]
        public struct PhysicMaterialMapping
        {
            public PhysicsMaterial material;
            public SurfaceType type;
        }

        [Serializable]
        public struct TerrainLayerMapping
        {
            public TerrainLayer layer;
            public SurfaceType type;
        }

        [Header("Efekty Cząsteczkowe (VFX) dla Podłoża")]
        public List<SurfaceVFXMapping> vfxMappings = new List<SurfaceVFXMapping>();

        [Serializable]
        public struct SurfaceVFXMapping
        {
            public SurfaceType type;
            public GameObject hitVFXPrefab;
            public GameObject footstepVFXPrefab;
        }
    }
}
