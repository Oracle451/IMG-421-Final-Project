using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A floating ocean base (oil-rig style). Has its own turrets and optionally
// spawns defending ships. When destroyed it gives a large gold reward.
// The central stronghold is a special instance of this with isCentralStronghold=true.
public class OceanBase : MonoBehaviour, IDamageable
{
    public bool IsAlive { get; private set; } = true;
    public ShipFaction Faction => ShipFaction.Enemy;

    [Header("Stats")]
    public float MaxHealth = 400f;
    public int   GoldReward = 250;
    public bool  IsCentralStronghold = false;

    [Header("Turret Children")]
    public List<CoastalTurret> Turrets = new(); // child turrets auto-collected if empty

    [Header("Defending Ships")]
    public GameObject DefenderPrefab;
    public int DefenderCount = 3;
    public float DefenderSpawnRadius = 12f;
    public float DefenderOrbitRadius = 22f;

    [Header("VFX")]
    public GameObject ExplosionVFX;
    public GameObject SinkingVFX;

    [Header("Health Bar")]
    public bool ShowHealthBar = true;
    public Vector3 HealthBarLocalOffset = new(0f, 3.2f, 0f);
    public Vector2 HealthBarSize = new(120f, 14f);

    // Runtime
    public float CurrentHealth { get; private set; }

    private Camera _mainCamera;
    private Slider _healthSlider;
    private Image _healthFill;
    private Canvas _healthCanvas;

    void Awake()
    {
        ApplyStructureLayerRecursively();
        EnsureDamageReceivers();
    }

    void Start()
    {
        CurrentHealth = MaxHealth;
        _mainCamera = Camera.main;

        // Auto-collect child turrets
        if (Turrets.Count == 0) Turrets.AddRange(GetComponentsInChildren<CoastalTurret>());

        if (ShowHealthBar)
            BuildHealthBar();

        UpdateHealthBar();
        SpawnDefenders();
    }

    void Update()
    {
        UpdateHealthBar();
    }

    void ApplyStructureLayerRecursively()
    {
        int structureLayer = LayerMask.NameToLayer("Structure");
        if (structureLayer < 0) return;
        SetLayerRecursively(gameObject, structureLayer);
    }

    void EnsureDamageReceivers()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            CoastalTurret turretParent = col.GetComponentInParent<CoastalTurret>();
            if (turretParent != null)
                continue;

            StructureHitReceiver receiver = col.GetComponent<StructureHitReceiver>();
            if (receiver == null)
                receiver = col.gameObject.AddComponent<StructureHitReceiver>();

            receiver.Type = StructureHitReceiver.StructureType.OceanBase;
            receiver.ExplicitBase = this;
        }
    }

    void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // Damage
    public void TakeDamage(float dmg)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
        UpdateHealthBar();
        if (CurrentHealth <= 0f) Die();
    }

    void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;
        CurrencyManager.Instance?.AddCurrency(GoldReward);

        if (SinkingVFX) Instantiate(SinkingVFX, transform.position, Quaternion.identity);
        if (ExplosionVFX)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 rndPos = transform.position + Random.insideUnitSphere * 5f;
                rndPos.y = transform.position.y;
                Instantiate(ExplosionVFX, rndPos, Quaternion.identity);
            }
        }

        if (IsCentralStronghold)
            GameManager.Instance?.OnStrongholdDestroyed();

        Destroy(gameObject, 0.5f);
    }

    // Defenders
    void SpawnDefenders()
    {
        if (DefenderPrefab == null) return;

        for (int i = 0; i < DefenderCount; i++)
        {
            float angle = (360f / DefenderCount) * i * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            );

            Vector3 pos = transform.position + direction * DefenderSpawnRadius;

            GameObject go = Instantiate(
                DefenderPrefab,
                pos,
                Quaternion.LookRotation(direction)
            );

            ShipBase ship = go.GetComponent<ShipBase>();
            if (ship != null)
            {
                ship.Faction = ShipFaction.Enemy;
                ship.ApplyFactionLayer();
            }

            EnemyShipAI ai = go.GetComponent<EnemyShipAI>();
            if (ai != null)
            {
                ai.DefenseAnchor = transform;
                ai.InitialState = EnemyShipAI.AIState.Defense;

                // Uses the AI field that already exists in your project.
                // This gives defenders a wider area around the base instead of collapsing into it.
                ai.PatrolRadius = DefenderOrbitRadius;
            }
        }
    }

    void BuildHealthBar()
    {
        GameObject canvasGO = new GameObject("OceanBaseHealthBar");
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
    }

    void UpdateHealthBar()
    {
        if (_healthSlider == null) return;

        float pct = MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        _healthSlider.value = pct;
        if (_healthFill != null)
            _healthFill.color = pct > 0.6f ? Color.green : pct > 0.3f ? Color.yellow : Color.red;

        if (_healthCanvas != null)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null)
                _healthCanvas.transform.rotation = _mainCamera.transform.rotation;
        }
    }

    // Projectile hit forwarding
    void OnCollisionEnter(Collision col)
    {
        Projectile proj = col.collider.GetComponent<Projectile>();
        // Projectile handles its own damage call via ShipBase.TakeDamage;
        // for structures we need direct forwarding, handled in Projectile via StructureHitReceiver.
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, DefenderSpawnRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, DefenderOrbitRadius);
    }
}