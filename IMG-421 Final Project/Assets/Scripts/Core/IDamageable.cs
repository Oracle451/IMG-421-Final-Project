// Shared interface for anything that can receive damage: ships, turrets, bases.
using UnityEngine;

public interface IDamageable
{
    bool IsAlive { get; }
    ShipFaction Faction { get; }
    Transform transform { get; }
    void TakeDamage(float dmg);
}
