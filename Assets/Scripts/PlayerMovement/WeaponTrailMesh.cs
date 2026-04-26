using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WeaponTrailMesh : MonoBehaviour
{
    [Header("Ustawienia Wizualne")]
    public Material trailMaterial;
    public float targetLifeTime = 0.2f; // Długość smugi
    public float fadeSpeed = 5f;        // Szybkość kurczenia
    public float minDistance = 0.01f;

    [Header("Płynność")]
    [Range(1, 10)]
    public int interpolationSteps = 5;

    [Header("Punkty Broni")]
    public Transform basePoint;
    public Transform tipPoint;

    private List<TrailPoint> _pointHistory = new List<TrailPoint>(256);
    private List<TrailPoint> _smoothedPoints = new List<TrailPoint>(1024);
    
    private List<Vector3> _vBuffer = new List<Vector3>(2048);
    private List<Vector2> _uBuffer = new List<Vector2>(2048);
    private List<int> _tBuffer = new List<int>(4096);

    private Mesh _trailMesh;
    private MeshFilter _mf;
    private MeshRenderer _mr;
    
    private bool _isEmitting;
    private float _currentLifeTime;

    private struct TrailPoint
    {
        public Vector3 BasePos;
        public Vector3 TipPos;
        public float TimeStamp;
    }

    private void Start()
    {
        // WYMUSZAMY odłączenie od miecza na starcie gry
        DetachFromParent();

        _mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        
        _trailMesh = new Mesh { name = "WeaponTrail_Pro" };
        _trailMesh.MarkDynamic();
        _mf.mesh = _trailMesh;
        
        if (trailMaterial != null) _mr.material = trailMaterial;
        _mr.enabled = false;
    }

    private void DetachFromParent()
    {
        if (transform.parent != null)
        {
            transform.parent = null;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }

    public void StartTrail()
    {
        // Na wypadek gdyby Awake/Start nie zadziałało (np. prefab)
        DetachFromParent();
        
        _isEmitting = true;
        _currentLifeTime = 0f;
        _pointHistory.Clear();
        _mr.enabled = true;
    }

    public void StopTrail() => _isEmitting = false;

    private void LateUpdate()
    {
        float now = Time.time;

        // 1. DYNAMIKA DŁUGOŚCI (Lustrzane odbicie / Zipper Effect)
        if (_isEmitting)
        {
            // ROZSZERZANIE: Smuga rośnie z miecza (OpenTrail)
            _currentLifeTime = Mathf.MoveTowards(_currentLifeTime, targetLifeTime, Time.deltaTime * (fadeSpeed * 0.5f));
        }
        else
        {
            // ZWIJANIE: Ogon smugi goni ostrze (CloseTrail)
            // Zmniejszamy okno czasu, co powoduje, że najstarsze punkty znikają szybciej
            _currentLifeTime = Mathf.MoveTowards(_currentLifeTime, 0f, Time.deltaTime * fadeSpeed);
        }

        // 2. PRZESYŁANIE ALPHY (Fade out)
        if (trailMaterial != null && _mr.enabled)
        {
            // Obliczamy zanikanie na podstawie aktualnego czasu życia względem docelowego
            float alpha = targetLifeTime > 0 ? (_currentLifeTime / targetLifeTime) : 0;
            _mr.material.SetFloat("_Fade", alpha); // Wysyłamy do shadera (dodaj parametr _Fade w Shader Graph!)
        }

        // 3. CZYSZCZENIE HISTORII (na podstawie dynamicznego okna _currentLifeTime)
        while (_pointHistory.Count > 0 && now - _pointHistory[0].TimeStamp > _currentLifeTime)
        {
            _pointHistory.RemoveAt(0);
        }

        // 4. ZBIERANIE PUNKTÓW (Tylko podczas emisji)
        if (_isEmitting && basePoint != null && tipPoint != null)
        {
            Vector3 b = basePoint.position;
            Vector3 t = tipPoint.position;

            if (_pointHistory.Count == 0 || Vector3.Distance(b, _pointHistory[_pointHistory.Count - 1].BasePos) > minDistance)
            {
                _pointHistory.Add(new TrailPoint { BasePos = b, TipPos = t, TimeStamp = now });
            }
        }

        // 5. GENEROWANIE I RENDEROWANIE
        GenerateSmoothedPath();
        RebuildMesh();

        // 6. AUTO-OFF (Kiedy smuga całkiem się "zwini")
        if (!_isEmitting && (_pointHistory.Count == 0 || _currentLifeTime <= 0.001f))
        {
            _mr.enabled = false;
        }
    }



    private void GenerateSmoothedPath()
    {
        _smoothedPoints.Clear();
        int count = _pointHistory.Count;
        if (count < 2) return;

        for (int i = 0; i < count - 1; i++)
        {
            int p0 = Mathf.Max(i - 1, 0);
            int p1 = i;
            int p2 = i + 1;
            int p3 = Mathf.Min(i + 2, count - 1);

            _smoothedPoints.Add(_pointHistory[p1]);

            for (int j = 1; j <= interpolationSteps; j++)
            {
                float t = j / (float)(interpolationSteps + 1);
                _smoothedPoints.Add(new TrailPoint
                {
                    BasePos = GetCatmullRom(t, _pointHistory[p0].BasePos, _pointHistory[p1].BasePos, _pointHistory[p2].BasePos, _pointHistory[p3].BasePos),
                    TipPos = GetCatmullRom(t, _pointHistory[p0].TipPos, _pointHistory[p1].TipPos, _pointHistory[p2].TipPos, _pointHistory[p3].TipPos),
                    TimeStamp = Mathf.Lerp(_pointHistory[p1].TimeStamp, _pointHistory[p2].TimeStamp, t)
                });
            }
        }
        _smoothedPoints.Add(_pointHistory[count - 1]);
    }

    private Vector3 GetCatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * ((2f * p1) + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
    }

    private void RebuildMesh()
    {
        _vBuffer.Clear(); _uBuffer.Clear(); _tBuffer.Clear();

        if (_smoothedPoints.Count < 2)
        {
            _trailMesh.Clear();
            return;
        }

        for (int i = 0; i < _smoothedPoints.Count; i++)
        {
            _vBuffer.Add(transform.InverseTransformPoint(_smoothedPoints[i].BasePos));
            _vBuffer.Add(transform.InverseTransformPoint(_smoothedPoints[i].TipPos));

            float uCoord = (float)i / (_smoothedPoints.Count - 1);
            _uBuffer.Add(new Vector2(uCoord, 0));
            _uBuffer.Add(new Vector2(uCoord, 1));

            if (i < _smoothedPoints.Count - 1)
            {
                int v = i * 2;
                _tBuffer.Add(v); _tBuffer.Add(v + 1); _tBuffer.Add(v + 2);
                _tBuffer.Add(v + 2); _tBuffer.Add(v + 1); _tBuffer.Add(v + 3);
            }
        }

        _trailMesh.Clear();
        _trailMesh.SetVertices(_vBuffer);
        _trailMesh.SetUVs(0, _uBuffer);
        _trailMesh.SetTriangles(_tBuffer, 0);
        _trailMesh.RecalculateNormals();
        _trailMesh.RecalculateBounds();
    }
}