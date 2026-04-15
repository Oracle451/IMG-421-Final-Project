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

    [Header("Placement")]
    public float VerticalOffset = 0.75f;
    public Vector2 BarSize = new(90f, 14f);

    private ShipBase _ship;
    private Camera   _cam;
    private Canvas   _canvas;
    private RectTransform _rect;
    private Bounds _visualBounds;

    void Start()
    {
        _ship = GetComponentInParent<ShipBase>();
        _cam  = Camera.main;
        _canvas = GetComponent<Canvas>();
        _rect = GetComponent<RectTransform>();
        _rect.localScale = Vector3.one * 0.06f;
        AutoWireReferences();
        EnsureWorldSpaceCanvas();
        CacheVisualBounds();
        EnsureVisiblePosition();

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

        EnsureVisiblePosition();

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

        if (HealthSlider == null)
            BuildRuntimeBar();
    }

    void EnsureWorldSpaceCanvas()
    {
        if (_canvas != null)
            _canvas.renderMode = RenderMode.WorldSpace;

        if (_rect != null)
        {
            if (_rect.localScale == Vector3.zero)
                _rect.localScale = Vector3.one * 0.01f;
            _rect.sizeDelta = BarSize;
        }
    }

    void CacheVisualBounds()
    {
        if (_ship == null) return;

        Renderer[] renderers = _ship.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            _visualBounds = new Bounds(_ship.transform.position, Vector3.one);
            return;
        }

        _visualBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            _visualBounds.Encapsulate(renderers[i].bounds);
    }

    void EnsureVisiblePosition()
    {
        if (_ship == null || _rect == null) return;

        float height = Mathf.Max(1f, _visualBounds.extents.y * 2f);
        Vector3 worldPos = _ship.transform.position + Vector3.up * (height + VerticalOffset);
        transform.position = worldPos;
    }

    void BuildRuntimeBar()
    {
        Sprite sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject sliderGO = new("Slider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(transform, false);
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.sizeDelta = BarSize;

        GameObject backgroundGO = new("Background", typeof(RectTransform), typeof(Image));
        backgroundGO.transform.SetParent(sliderGO.transform, false);
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundGO.GetComponent<Image>();
        backgroundImage.sprite = sprite;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject fillAreaGO = new("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fillGO = new("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fill = fillGO.GetComponent<Image>();
        fill.sprite = sprite;
        fill.type = Image.Type.Sliced;
        fill.color = FullColor;

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = fill;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        HealthSlider = slider;
        FillImage = fill;
    }
}
