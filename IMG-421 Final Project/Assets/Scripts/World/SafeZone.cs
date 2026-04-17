using UnityEngine;

// A trigger volume marking a safe zone. Enemies won't spawn inside it.
// Enables the shop UI when the player's fleet enters.
[RequireComponent(typeof(Collider))]
public class SafeZone : MonoBehaviour
{
    [Header("Visual")]
    public Renderer ZoneRenderer; // optional semi-transparent zone indicator mesh
    public Color SafeColor = new Color(0.2f, 0.8f, 0.2f, 0.15f);

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (ZoneRenderer != null)
        {
            Material mat = ZoneRenderer.material;
            mat.color = SafeColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        ShipBase ship = other.GetComponentInParent<ShipBase>();
        if (ship != null && ship.Faction == ShipFaction.Player)
        {
            UIManager.Instance?.ToggleShop();
        }
    }

    void OnTriggerExit(Collider other)
    {
        ShipBase ship = other.GetComponentInParent<ShipBase>();
        if (ship != null && ship.Faction == ShipFaction.Player)
        {
            if (UIManager.Instance?.ShopPanel?.activeSelf == true) UIManager.Instance?.ToggleShop();
        }
    }
}
