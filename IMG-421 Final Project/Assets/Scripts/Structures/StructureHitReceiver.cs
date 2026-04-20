using UnityEngine;

// Attach to any structure (OceanBase, CoastalTurret) child collider GameObject
// so that Projectile.OnCollisionEnter can route damage to the intended structure.
public class StructureHitReceiver : MonoBehaviour
{
    public enum StructureType { OceanBase, CoastalTurret }
    public StructureType Type;

    [System.NonSerialized] public OceanBase ExplicitBase;
    [System.NonSerialized] public CoastalTurret ExplicitTurret;

    private OceanBase _base;
    private CoastalTurret _turret;

    void Awake()
    {
        ResolveTargets();
    }

    void OnEnable()
    {
        ResolveTargets();
    }

    void ResolveTargets()
    {
        if (ExplicitBase == null)
            _base = GetComponentInParent<OceanBase>();
        else
            _base = ExplicitBase;

        if (ExplicitTurret == null)
        {
            // Important: some turret prefabs contain nested CoastalTurret components.
            // Use the top-most enabled turret so projectile damage reaches the live root turret,
            // not a disabled child component.
            CoastalTurret[] turrets = GetComponentsInParent<CoastalTurret>(true);
            _turret = null;
            for (int i = turrets.Length - 1; i >= 0; i--)
            {
                CoastalTurret candidate = turrets[i];
                if (candidate != null && candidate.enabled)
                {
                    _turret = candidate;
                    break;
                }
            }

            if (_turret == null && turrets.Length > 0)
                _turret = turrets[turrets.Length - 1];
        }
        else
        {
            _turret = ExplicitTurret;
        }
    }

    public void ReceiveDamage(float dmg)
    {
        if ((Type == StructureType.OceanBase && _base == null) ||
            (Type == StructureType.CoastalTurret && _turret == null))
        {
            ResolveTargets();
        }

        switch (Type)
        {
            case StructureType.OceanBase:
                _base?.TakeDamage(dmg);
                break;
            case StructureType.CoastalTurret:
                _turret?.TakeDamage(dmg);
                break;
        }
    }
}
