using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WeaponTrailMesh : MonoBehaviour
{
    [Header("Ustawienia Wizualne")]
    public Material trailMaterial;
    public float targetLifeTime = 0.2f; // Stała długość "ogona"
    public float fadeSpeed = 5f;        // Szybkość pojawiania/znikania (alpha)
    public float minDistance = 0.01f;

    [Header("Płynność")]
    [Range(1, 30)]
    public int interpolationSteps = 10;

    [Header("Punkty Broni")]
    public List<Transform> sourcePoints = new List<Transform>();

    private List<TrailPoint> _pointHistory = new List<TrailPoint>(256);
    private List<TrailPoint> _smoothedPoints = new List<TrailPoint>(1024);
    
    private List<Vector3> _vBuffer = new List<Vector3>(2048);
    private List<Vector2> _uBuffer = new List<Vector2>(2048);
    private List<int> _tBuffer = new List<int>(4096);

    private Mesh _trailMesh;
    private MeshFilter _mf;
    private MeshRenderer _mr;
    
    private bool _isEmitting;
    private float _currentAlpha;

    private struct TrailPoint
    {
        public Vector3[] Positions;
        public float TimeStamp;
    }

    private void Start()
    {
        DetachFromParent();

        _mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        
        _trailMesh = new Mesh { name = "WeaponTrail_MultiPoint" };
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
        DetachFromParent();
        _isEmitting = true;
        _currentAlpha = 0f;
        _pointHistory.Clear();
        _mr.enabled = true;
    }

    public void StopTrail() => _isEmitting = false;

    public void SetMaterial(Material mat)
    {
        trailMaterial = mat;
        if (_mr != null) _mr.material = mat;
    }

    private void LateUpdate()
    {
        float now = Time.time;

        // 1. FADE
        if (_isEmitting)
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 1f, Time.deltaTime * fadeSpeed);
        else
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, Time.deltaTime * fadeSpeed);

        if (trailMaterial != null && _mr.enabled)
        {
            _mr.material.SetFloat("_Fade", _currentAlpha);
        }

        // 2. USUWANIE STARYCH PUNKTÓW
        while (_pointHistory.Count > 0 && now - _pointHistory[0].TimeStamp > targetLifeTime)
        {
            _pointHistory.RemoveAt(0);
        }

        // 3. ZBIERANIE PUNKTÓW
        if (_isEmitting && sourcePoints != null && sourcePoints.Count >= 2 && _mr.enabled)
        {
            Vector3[] currentPositions = new Vector3[sourcePoints.Count];
            for (int i = 0; i < sourcePoints.Count; i++)
            {
                if (sourcePoints[i] != null) currentPositions[i] = sourcePoints[i].position;
            }

            // Sprawdzamy dystans tylko po ostatnim punkcie (tip)
            Vector3 lastTip = currentPositions[currentPositions.Length - 1];
            if (_pointHistory.Count == 0 || Vector3.Distance(lastTip, _pointHistory[_pointHistory.Count - 1].Positions[currentPositions.Length - 1]) > minDistance)
            {
                _pointHistory.Add(new TrailPoint { Positions = currentPositions, TimeStamp = now });
            }
        }

        // 4. RENDEROWANIE
        if (_pointHistory.Count > 1)
        {
            GenerateSmoothedPath();
            RebuildMesh();
        }

        // 5. AUTO-OFF
        if (!_isEmitting && _currentAlpha <= 0.001f)
        {
            _mr.enabled = false;
            _pointHistory.Clear();
            _trailMesh.Clear();
        }
    }

    private void GenerateSmoothedPath()
    {
        _smoothedPoints.Clear();
        int historyCount = _pointHistory.Count;
        if (historyCount < 2) return;

        int pointsPerStep = sourcePoints.Count;

        for (int i = 0; i < historyCount - 1; i++)
        {
            _smoothedPoints.Add(_pointHistory[i]);

            int p0 = Mathf.Max(i - 1, 0);
            int p1 = i;
            int p2 = i + 1;
            int p3 = Mathf.Min(i + 2, historyCount - 1);

            for (int j = 1; j <= interpolationSteps; j++)
            {
                float t = j / (float)(interpolationSteps + 1);
                Vector3[] interpPositions = new Vector3[pointsPerStep];

                for (int k = 0; k < pointsPerStep; k++)
                {
                    interpPositions[k] = GetCatmullRom(t, 
                        _pointHistory[p0].Positions[k], 
                        _pointHistory[p1].Positions[k], 
                        _pointHistory[p2].Positions[k], 
                        _pointHistory[p3].Positions[k]);
                }

                _smoothedPoints.Add(new TrailPoint
                {
                    Positions = interpPositions,
                    TimeStamp = Mathf.Lerp(_pointHistory[p1].TimeStamp, _pointHistory[p2].TimeStamp, t)
                });
            }
        }
        _smoothedPoints.Add(_pointHistory[historyCount - 1]);
    }

    private Vector3 GetCatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * ((2f * p1) + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
    }

    private void RebuildMesh()
    {
        _vBuffer.Clear(); _uBuffer.Clear(); _tBuffer.Clear();

        var historySteps = _smoothedPoints;
        int pointsPerStep = sourcePoints.Count;

        for (int i = 0; i < historySteps.Count; i++)
        {
            for (int k = 0; k < pointsPerStep; k++)
            {
                _vBuffer.Add(transform.InverseTransformPoint(historySteps[i].Positions[k]));
                
                float uCoord = (float)i / (historySteps.Count - 1);
                float vCoord = (float)k / (pointsPerStep - 1);
                _uBuffer.Add(new Vector2(uCoord, vCoord));
            }

            if (i < historySteps.Count - 1)
            {
                for (int k = 0; k < pointsPerStep - 1; k++)
                {
                    int baseIdx = i * pointsPerStep + k;
                    int nextStepIdx = (i + 1) * pointsPerStep + k;

                    // Triangle 1
                    _tBuffer.Add(baseIdx); 
                    _tBuffer.Add(baseIdx + 1); 
                    _tBuffer.Add(nextStepIdx);

                    // Triangle 2
                    _tBuffer.Add(nextStepIdx); 
                    _tBuffer.Add(baseIdx + 1); 
                    _tBuffer.Add(nextStepIdx + 1);
                }
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