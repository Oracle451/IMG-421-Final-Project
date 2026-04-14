/// <summary>
/// Shared interface for anything that can receive damage: ships, turrets, bases.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
    bool IsAlive { get; }
}
