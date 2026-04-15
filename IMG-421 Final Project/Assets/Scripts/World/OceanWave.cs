using UnityEngine;

/// <summary>
/// Applies Perlin-noise-based vertex displacement to a plane mesh to simulate ocean waves.
/// Attach to a large plane GameObject that acts as the ocean surface.
/// The mesh must be high-res enough to show wave detail (at least 100x100 subdivisions).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanWave : MonoBehaviour
{
    [Header("Wave Parameters")]
    public float WaveHeight    = 0.4f;
    public float WaveFrequency = 0.3f;
    public float WaveSpeed     = 0.5f;

    [Header("Secondary Wave (layered)")]
    public float Wave2Height    = 0.15f;
    public float Wave2Frequency = 0.8f;
    public float Wave2Speed     = 0.9f;
    public float Wave2Direction = 45f;  // degrees offset

    private Mesh _mesh;
    private Vector3[] _baseVertices;
    private Vector3[] _modifiedVertices;
    private Color[] _colors;
    public Color WaterColor = new Color(18f, 116f, 202f);

    void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        _mesh.MarkDynamic();
        _baseVertices     = _mesh.vertices;
        _modifiedVertices = new Vector3[_baseVertices.Length];
        _colors = new Color[_baseVertices.Length];
    }

    void Update()
    {
        float t   = Time.time;
        float dir = Wave2Direction * Mathf.Deg2Rad;

        for (int i = 0; i < _baseVertices.Length; i++)
        {
            Vector3 v  = _baseVertices[i];
            float wx   = v.x * WaveFrequency + t * WaveSpeed;
            float wz   = v.z * WaveFrequency + t * WaveSpeed;
            float wave1 = Mathf.PerlinNoise(wx, wz) * WaveHeight;

            float wx2   = (v.x * Mathf.Cos(dir) - v.z * Mathf.Sin(dir)) * Wave2Frequency + t * Wave2Speed;
            float wz2   = (v.x * Mathf.Sin(dir) + v.z * Mathf.Cos(dir)) * Wave2Frequency + t * Wave2Speed;
            float wave2 = Mathf.PerlinNoise(wx2, wz2) * Wave2Height;

            float height = wave1 + wave2;

            _modifiedVertices[i] = new Vector3(v.x, height, v.z);

            // Foam calculation
            float foam = Mathf.InverseLerp(0.2f, 0.5f, height); 
            foam = Mathf.Clamp01(foam);

            // Blend blue to white
            _colors[i] = Color.Lerp(WaterColor, Color.white, foam);
        }

        _mesh.colors = _colors;
        _mesh.vertices = _modifiedVertices;
        _mesh.RecalculateNormals();
    }
}
