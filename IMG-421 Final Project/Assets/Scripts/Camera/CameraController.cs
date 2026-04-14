using UnityEngine;

/// <summary>
/// Top-down camera that smoothly follows the player fleet centroid.
/// Scroll wheel or dedicated key zooms out/in.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    public float SmoothTime     = 0.2f;

    [Header("Height / Zoom")]
    public float DefaultHeight  = 30f;
    public float MinHeight      = 12f;
    public float MaxHeight      = 60f;
    public float ZoomSpeed      = 5f;
    public float ZoomOutHoldHeight = 55f;   // height when zoom-out key held

    [Header("Angle")]
    public float Pitch = 55f;   // degrees above horizon

    [Header("Key Bindings")]
    public KeyCode ZoomOutKey = KeyCode.Space;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private Vector3 _velocity = Vector3.zero;
    private float   _targetHeight;

    void Start() => _targetHeight = DefaultHeight;

    void LateUpdate()
    {
        // Determine target
        Vector3 fleetCenter = GameManager.Instance?.PlayerFleet?.FleetCenter() ?? Vector3.zero;

        // Scroll wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _targetHeight = Mathf.Clamp(_targetHeight - scroll * ZoomSpeed * 10f, MinHeight, MaxHeight);

        // Hold key zoom-out
        if (Input.GetKey(ZoomOutKey))
            _targetHeight = Mathf.MoveTowards(_targetHeight, ZoomOutHoldHeight, ZoomSpeed * 2f * Time.deltaTime);
        else
            _targetHeight = Mathf.MoveTowards(_targetHeight, DefaultHeight, ZoomSpeed * Time.deltaTime);

        // Desired world position: above fleet, pulled back by pitch
        float pitchRad = Pitch * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(0,
            _targetHeight,
           -_targetHeight / Mathf.Tan(pitchRad));

        Vector3 desired = fleetCenter + offset;

        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, SmoothTime);

        // Look at fleet center
        transform.LookAt(fleetCenter);
    }
}
