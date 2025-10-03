using System;
using UnityEngine;

public class UICrosshair : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    public CrosshairType crosshairType;
    
    void Start()
    {
        Cursor.visible = false;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        var visibilityState = crosshairType switch
        {
            CrosshairType.Game => !LevelManager.IsPaused() && !LevelManager.Player.IsMovementBlocked(),
            CrosshairType.PauseMenu => LevelManager.IsPauseMenuActive(),
            _ => false
        };
        
        canvasGroup.alpha = visibilityState ? 1 : 0;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            Input.mousePosition,
            rectTransform.GetComponentInParent<Canvas>()?.worldCamera,
            out var localPoint
        );
        rectTransform.localPosition = localPoint;
    }

    [Serializable]
    public enum CrosshairType
    {
        Game,
        PauseMenu,
    }
}