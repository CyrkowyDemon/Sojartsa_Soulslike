using UnityEngine;

namespace SojartsaAI.v3
{
    /// <summary>
    /// Opcjonalny moduł dla przeciwników chodzących. 
    /// Odpowiada za wykrywanie podłoża i efekty kroków.
    /// </summary>
    public class EnemyFootstepManager : MonoBehaviour
    {
        private EnemySFXManager _sfxManager;
        
        [Header("Ustawienia")]
        [SerializeField] private float raycastDistance = 1.0f;
        [SerializeField] private LayerMask groundMask = ~0; // Wszystko domyślnie

        private void Awake()
        {
            _sfxManager = GetComponentInParent<EnemySFXManager>();
        }

        /// <summary>
        /// Wywoływane przez Animation Event lub Relay.
        /// </summary>
        public void PlayFootstep()
        {
            if (_sfxManager == null) return;

            // Wykrywamy podłoże dokładnie pod obiektem (np. pod nogą)
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            RaycastHit hit;
            
            Sojartsa.Systems.Surface.SurfaceType surfaceType = Sojartsa.Systems.Surface.SurfaceType.Default;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundMask))
            {
                // Jeśli trafiliśmy w coś, pytamy SurfaceManager co to jest
                if (Sojartsa.Systems.Surface.SurfaceManager.Instance != null)
                {
                    surfaceType = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetSurface(hit.point);
                    
                    // Efekt wizualny (VFX)
                    GameObject vfx = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetFootstepVFX(surfaceType);
                    if (vfx != null)
                    {
                        GameObject spawnedVFX = SimplePool.Spawn(vfx, hit.point, Quaternion.LookRotation(Vector3.up));
                        SimplePool.Despawn(spawnedVFX, vfx, 2.0f);
                    }
                }
            }

            // Przekazujemy info do SFX Managera, żeby zagrał dźwięk z odpowiednim parametrem
            _sfxManager.PlayFootstepWithSurface(hit.point, surfaceType.ToString());
        }
    }
}
