using UnityEngine;

/// <summary>
/// Dynamically fades between ambient sea music and intense combat music
/// based on whether any enemies are within the player fleet's detection radius.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DynamicMusicManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip AmbientClip;
    public AudioClip CombatClip;

    [Header("Blend Settings")]
    public float FadeSpeed          = 1.5f;
    public float CombatDetectRadius = 35f;
    public LayerMask EnemyLayer;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private AudioSource _ambientSource;
    private AudioSource _combatSource;
    private bool        _inCombat;

    void Awake()
    {
        // Use two AudioSource components — add one dynamically
        _ambientSource       = gameObject.AddComponent<AudioSource>();
        _combatSource        = gameObject.AddComponent<AudioSource>();

        _ambientSource.clip  = AmbientClip;
        _combatSource.clip   = CombatClip;

        _ambientSource.loop  = true;
        _combatSource.loop   = true;

        _ambientSource.volume = 1f;
        _combatSource.volume  = 0f;

        _ambientSource.Play();
        _combatSource.Play();
    }

    void Update()
    {
        Vector3 fleetCenter = GameManager.Instance?.PlayerFleet?.FleetCenter() ?? Vector3.zero;
        Collider[] nearby   = Physics.OverlapSphere(fleetCenter, CombatDetectRadius, EnemyLayer);
        _inCombat           = nearby.Length > 0;

        float targetAmbient = _inCombat ? 0f : 1f;
        float targetCombat  = _inCombat ? 1f : 0f;

        _ambientSource.volume = Mathf.MoveTowards(_ambientSource.volume, targetAmbient, FadeSpeed * Time.deltaTime);
        _combatSource.volume  = Mathf.MoveTowards(_combatSource.volume,  targetCombat,  FadeSpeed * Time.deltaTime);
    }
}
