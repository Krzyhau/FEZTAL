using UnityEngine;
using UnityEngine.UI;

public class PortalShooterIndicator : MonoBehaviour
{
    public int portalIndex = 0;
    public float inactiveTransparency = 0.5f;
    
    
    PortalShooter portalShooter;
    Image indicator;

    void Start()
    {
        indicator = GetComponent<Image>();
        portalShooter = LevelManager.Player?.GetComponent<PortalShooter>();
    }
    
    void Update()
    {
        if (portalShooter == null)
        {
            return;
        }

        var portalActive = portalShooter.GetPortal(portalIndex)?.enabled ?? false;
        var color = portalShooter.GetPortalColor(portalIndex);

        if (!portalActive)
        {
            color.a = inactiveTransparency;
        }
        
        indicator.color = color;
    }
}