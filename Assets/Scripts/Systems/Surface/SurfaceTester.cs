using UnityEngine;
using Sojartsa.Systems.Surface;

namespace Sojartsa.Systems.DebugTools
{
    public class SurfaceTester : MonoBehaviour
    {
        void Update()
        {
            if (SurfaceManager.Instance == null) return;

            // Sprawdzamy co mamy pod nogami
            SurfaceType currentSurface = SurfaceManager.Instance.GetSurface(transform.position);

            // Rysujemy debugowy promień
            Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 1f, GetDebugColor(currentSurface));
            
            // Wypisujemy tylko gdy się zmieniło (żeby nie śmiecić)
            if (Input.GetKeyDown(KeyCode.P)) 
            {
                Debug.Log($"<color=yellow>[SurfaceTest]</color> Stoisz na: <b>{currentSurface}</b>");
            }
        }

        private Color GetDebugColor(SurfaceType type)
        {
            switch (type)
            {
                case SurfaceType.Grass: return Color.green;
                case SurfaceType.Stone: return Color.gray;
                case SurfaceType.Wood: return Color.red;
                case SurfaceType.Metal: return Color.cyan;
                case SurfaceType.Water: return Color.blue;
                default: return Color.white;
            }
        }
    }
}
