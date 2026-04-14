using UnityEngine;

/// <summary>
/// Attach to any structure (OceanBase, CoastalTurret) child collider GameObject
/// so that Projectile.OnCollisionEnter can route damage to the parent structure.
/// </summary>
public class StructureHitReceiver : MonoBehaviour
{
    public enum StructureType { OceanBase, CoastalTurret }
    public StructureType Type;

    private OceanBase     _base;
    private CoastalTurret _turret;

    void Awake()
    {
        _base   = GetComponentInParent<OceanBase>();
        _turret = GetComponentInParent<CoastalTurret>();
    }

    public void ReceiveDamage(float dmg)
    {
        switch (Type)
        {
            case StructureType.OceanBase:     _base?.TakeDamage(dmg);   break;
            case StructureType.CoastalTurret: _turret?.TakeDamage(dmg); break;
        }
    }
}
