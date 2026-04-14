using UnityEngine;

/// <summary>
/// Central input manager. Exposes named axes and button states
/// so other systems don't hardcode Input calls directly.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Key Bindings")]
    public KeyCode PauseKey      = KeyCode.Escape;
    public KeyCode ZoomOutKey    = KeyCode.Space;
    public KeyCode ToggleShopKey = KeyCode.B;
    public KeyCode DeselectKey   = KeyCode.Q;

    // ── Cached per-frame state ────────────────────────────────────────────────
    public bool PausePressed      { get; private set; }
    public bool ZoomOutHeld       { get; private set; }
    public bool ToggleShopPressed { get; private set; }
    public bool DeselectPressed   { get; private set; }
    public bool RightClickDown    { get; private set; }
    public bool LeftClickDown     { get; private set; }
    public float ScrollDelta      { get; private set; }
    public Vector3 MouseWorld     { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        PausePressed      = Input.GetKeyDown(PauseKey);
        ZoomOutHeld       = Input.GetKey(ZoomOutKey);
        ToggleShopPressed = Input.GetKeyDown(ToggleShopKey);
        DeselectPressed   = Input.GetKeyDown(DeselectKey);
        RightClickDown    = Input.GetMouseButtonDown(1);
        LeftClickDown     = Input.GetMouseButtonDown(0);
        ScrollDelta       = Input.GetAxis("Mouse ScrollWheel");

        // Pause toggle
        if (PausePressed) GameManager.Instance?.TogglePause();
        if (ToggleShopPressed) UIManager.Instance?.ToggleShop();
        if (DeselectPressed)   GameManager.Instance?.PlayerFleet?.DeselectShip();
    }
}
