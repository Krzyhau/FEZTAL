using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FadeScript : MonoBehaviour
{
    [SerializeField] private Image _fadeImage;
    [Header("Properties")]
    public Color FadeColor;
    public float FadeTime;
    [SerializeField] private bool _fadeInOnStart;
    [SerializeField] private bool _fadeOutOnStart;

    private float _fadeTarget = 0.0f;
    private float _fadeValue = 0.0f;

    private void Start()
    {
        if (_fadeInOnStart) FadeIn();
        if (_fadeOutOnStart) FadeOut();
    }

    private void Update()
    {
        if(_fadeValue != _fadeTarget)
        {
            _fadeValue = Mathf.MoveTowards(_fadeValue, _fadeTarget, (1.0f / FadeTime) * Time.deltaTime);
            FadeColor.a = _fadeValue;
            _fadeImage.color = FadeColor;
        }
    }

    // fades into the color
    public void FadeIn()
    { 
        _fadeValue = 0.0f;
        _fadeTarget = 1.0f;
    }

    // fades out of the color
    public void FadeOut()
    {
        _fadeValue = 1.0f;
        _fadeTarget = 0.0f;
    }
}
