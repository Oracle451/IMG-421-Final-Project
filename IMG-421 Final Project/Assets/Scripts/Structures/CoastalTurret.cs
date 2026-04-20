using UnityEngine;
using UnityEngine.UI;

// A static coastal turret mounted on an island.
// Rotates to track and fire at the nearest player ship within range.
public class CoastalTurret : MonoBehaviour, IDamageable
{
    public bool IsAlive { get; private set; } = true;
    public ShipFaction Faction => ShipFaction.Enemy;
    public float CurrentHealth => _currentHealth;

    [Header("Stats")]
    public float MaxHealth = 60f;
    public float Damage = 8f;
    public float Range = 18f;
    public float FireRate = 0.6f; // shots per second
    public float RotationSpeed = 80f; // degrees/sec
    public int GoldReward = 30;

    [Header("References")]
    public GameObject ProjectilePrefab;
    public GameObject ExplosionVFX;
    public float ProjectileSpawnForwardOffset = 1.5f;

    [Header("Layer Mask")]
    public LayerMask PlayerLayer;

    [Header("Health Bar")]
    public bool ShowHealthBar = true;
    public Vector3 HealthBarLocalOffset = new(0f, 1.5f, 0f);
    public Vector2 HealthBarSize = new(60f, 10f);

    // Runtime
    private float _currentHealth;
    private float _fireCooldown;
    private Collider[] _ignoredColliders;
    private Camera _mainCamera;
    private Slider _healthSlider;
    private Image _healthFill;
    private Canvas _healthCanvas;
    private bool _runtimeDisabled;

    void Awake()
    {
        // Some prefab instances contain nested CoastalTurret components.
        // Keep only the top-most turret logic active so damage, targeting,
        // and death all route to the visible/intended turret root.
        CoastalTurret parentTurret = transform.parent != null
            ? transform.parent.GetComponentInParent<CoastalTurret>(true)
            : null;
        if (parentTurret != null)
        {
            _runtimeDisabled = true;
            enabled = false;
            return;
        }

        ApplyStructureLayerRecursively();
        EnsureDamageReceivers();
    }

    void Start()
    {
        if (_runtimeDisabled) return;

        _currentHealth = MaxHealth;
        _mainCamera = Camera.main;
        EnsurePlayerLayer();

        // Ignore collisions with all CoastalTurret colliders in the scene.
        // This prevents turret shots from colliding with nearby structure geometry.
        CoastalTurret[] allTurrets = FindObjectsByType<CoastalTurret>(FindObjectsSortMode.None);
        int total = 0;
        foreach (CoastalTurret t in allTurrets)
        {
            if (t == null || t._runtimeDisabled) continue;
            total += t.GetComponentsInChildren<Collider>(true).Length;
        }

        _ignoredColliders = new Collider[total];
        int idx = 0;
        foreach (CoastalTurret t in allTurrets)
        {
            if (t == null || t._runtimeDisabled) continue;
            foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
                _ignoredColliders[idx++] = c;
        }

        if (ShowHealthBar)
            BuildHealthBar();
    }

    void Update()
    {
        if (_runtimeDisabled || !IsAlive) return;

        _fireCooldown -= Time.deltaTime;

        ShipBase target = FindPlayerShip();
        if (target != null)
        {
            // Rotate this transform toward target on Y axis only
            Vector3 dir = (target.transform.position - transform.position);
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                Quaternion desired = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, RotationSpeed * Time.deltaTime);
            }

            if (_fireCooldown <= 0f)
            {
                _fireCooldown = 1f / FireRate;
                FireAt(target);
            }
        }

        UpdateHealthBar();
    }

    void ApplyStructureLayerRecursively()
    {
        int structureLayer = LayerMask.NameToLayer("Structure");
        if (structureLayer < 0) return;
        SetLayerRecursively(gameObject, structureLayer);
    }

    void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    void EnsurePlayerLayer()
    {
        int playerLayer = LayerMask.NameToLayer("PlayerShip");
        if (playerLayer < 0) return;

        int playerBit = 1 << playerLayer;
        if ((PlayerLayer.value & playerBit) == 0)
            PlayerLayer = LayerMask.GetMask("PlayerShip");
    }

    void EnsureDamageReceivers()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            StructureHitReceiver receiver = col.GetComponent<StructureHitReceiver>();
            if (receiver == null)
                receiver = col.gameObject.AddComponent<StructureHitReceiver>();

            receiver.Type = StructureHitReceiver.StructureType.CoastalTurret;
            receiver.ExplicitTurret = this;
        }
    }

    // Targeting
    ShipBase FindPlayerShip()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, Range, PlayerLayer);
        ShipBase best = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            ShipBase s = col.GetComponentInParent<ShipBase>();
            if (s == null || !s.IsAlive || s.Faction != ShipFaction.Player) continue;
            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d < bestDist) { bestDist = d; best = s; }
        }
        return best;
    }

    // Firing
    void FireAt(ShipBase target)
    {
        if (ProjectilePrefab == null) return;

        Vector3 launchOrigin = transform.position + Vector3.up;
        Vector3 dir = target.transform.position - launchOrigin;
        dir.y = 0f;
        if (dir == Vector3.zero) return;
        dir.Normalize();

        Vector3 spawnPos = launchOrigin + dir * ProjectileSpawnForwardOffset;
        GameObject projGO = Instantiate(ProjectilePrefab, spawnPos, Quaternion.LookRotation(dir));
        Projectile proj = projGO.GetComponent<Projectile>();
        proj?.Launch(dir * 22f, Damage, ShipFaction.Enemy, _ignoredColliders);
    }

    // Damage
    public void TakeDamage(float dmg)
    {
        if (_runtimeDisabled || !IsAlive) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - dmg);
        UpdateHealthBar();

        if (_currentHealth <= 0f) Die();
    }

    void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;
        CurrencyManager.Instance?.AddCurrency(GoldReward);
        if (ExplosionVFX) Instantiate(ExplosionVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void BuildHealthBar()
    {
        GameObject canvasGO = new GameObject("TurretHealthBar");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = HealthBarLocalOffset;

        _healthCanvas = canvasGO.AddComponent<Canvas>();
        _healthCanvas.renderMode = RenderMode.WorldSpace;
        _healthCanvas.sortingOrder = 100;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = HealthBarSize;
        canvasRect.localScale = Vector3.one * 0.01f;

        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        _healthSlider = sliderGO.AddComponent<Slider>();
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(1f, 1f);
        sliderRect.offsetMax = new Vector2(-1f, -1f);
        _healthSlider.transition = Selectable.Transition.None;
        _healthSlider.minValue = 0f;
        _healthSlider.maxValue = 1f;
        _healthSlider.value = 1f;
        _healthSlider.direction = Slider.Direction.LeftToRight;

        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        _healthFill = fillGO.AddComponent<Image>();
        _healthFill.color = Color.green;
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        _healthSlider.fillRect = fillRect;
        _healthSlider.targetGraphic = _healthFill;

        GameObject handleSlideArea = new GameObject("Handle Slide Area");
        handleSlideArea.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleSlideArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleSlideArea.transform, false);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0, 0, 0, 0);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = Vector2.zero;
        _healthSlider.handleRect = handleRect;

        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (_healthSlider == null) return;

        float pct = MaxHealth > 0f ? _currentHealth / MaxHealth : 0f;
        _healthSlider.value = pct;

        if (_healthFill != null)
        {
            _healthFill.color = pct > 0.6f ? Color.green : pct > 0.3f ? Color.yellow : Color.red;
        }

        if (_healthCanvas != null)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null)
                _healthCanvas.transform.rotation = _mainCamera.transform.rotation;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}
