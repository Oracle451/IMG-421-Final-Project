using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar rendered above each ship via a Canvas in World Space.
/// Attach to a child GameObject of the ship that has a Canvas (World Space) and Slider.
/// </summary>
public class ShipHealthBar : MonoBehaviour
{
    [Header("References")]
    public Slider HealthSlider;
    public Image  FillImage;

    [Header("Colors")]
    public Color FullColor   = Color.green;
    public Color MidColor    = Color.yellow;
    public Color LowColor    = Color.red;

    [Header("Billboard")]
    public bool FaceCamera = true;

    private ShipBase _ship;
    private Camera   _cam;

    void Start()
    {
        _ship = GetComponentInParent<ShipBase>();
        _cam  = Camera.main;
        AutoWireReferences();
        EnsureWorldSpaceCanvas();

        if (HealthSlider != null)
        {
            HealthSlider.minValue = 0f;
            HealthSlider.maxValue = 1f;
            HealthSlider.value    = 1f;
        }
    }

    void LateUpdate()
    {
        if (_ship == null) return;

        // Update fill
        float pct = _ship.EffectiveMaxHealth > 0f
            ? _ship.CurrentHealth / _ship.EffectiveMaxHealth
            : 0f;

        if (HealthSlider) HealthSlider.value = pct;

        if (FillImage)
        {
            FillImage.color = pct > 0.6f ? FullColor
                            : pct > 0.3f ? MidColor
                            :              LowColor;
        }

        // Billboard — face camera
        if (FaceCamera && _cam != null)
            transform.rotation = _cam.transform.rotation;
    }

    void AutoWireReferences()
    {
        if (HealthSlider == null)
            HealthSlider = GetComponentInChildren<Slider>(true);

        if (FillImage == null && HealthSlider != null && HealthSlider.fillRect != null)
            FillImage = HealthSlider.fillRect.GetComponent<Image>();
    }

    void EnsureWorldSpaceCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        RectTransform rect = GetComponent<RectTransform>();
        if (canvas != null)
            canvas.renderMode = RenderMode.WorldSpace;

        if (rect != null && rect.localScale == Vector3.zero)
            rect.localScale = Vector3.one * 0.01f;
    }
}
