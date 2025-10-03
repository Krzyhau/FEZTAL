using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedrunTimer : MonoBehaviour
{
    [SerializeField] private CanvasGroup _hudGroup;
    [SerializeField] private Text _valuesText;

    public bool Active;

    private void Update()
    {
        if (!LevelManager.IsPaused() && SpeedrunValues.timerActive)
        {
            SpeedrunValues.timer += Time.unscaledDeltaTime;
        }

        UpdateHud();
    }

    private void UpdateHud()
    {
        bool shouldShow = Active && !LevelManager.IsPaused() && LevelManager.PauseMenu.allowPausing;
        if (shouldShow && _hudGroup.alpha < 1.0f) _hudGroup.alpha = 1.0f;
        if (!shouldShow && _hudGroup.alpha > 0.0f) _hudGroup.alpha = 0.0f;

        if (SpeedrunValues.timerActive)
        {
            string values = "";
            float srtime = SpeedrunValues.timer;
            values += $"{Mathf.FloorToInt(srtime / 60)}:{Mathf.FloorToInt(srtime % 60).ToString("D2")}";
            if (srtime < 600)
            { // smaller than 10 minutes
                values += $".{Mathf.FloorToInt((srtime * 1000) % 1000).ToString("D3")}";
            }
            values += $" \n{SpeedrunValues.portalCount} \n{SpeedrunValues.shiftCount} ";

            _valuesText.text = values;
        }
    }
}
