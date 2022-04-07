using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalDoor: Passage
{
    [Header("Portal-door related stuff")]
    [SerializeField] private AnimationCurve _openingCurve;
    [SerializeField] private float _openingSpeed;
    [SerializeField] private bool _useExitDoorTexture;

    private float _openingState = 0.0f;
    private Material _doorMaterial;

    void Start()
    {
        if (IsOpened) _openingState = 1.0f;
        _doorMaterial = transform.GetChild(0).GetComponent<MeshRenderer>().materials[0];
    }

    void Update()
    {
        _openingState = Mathf.MoveTowards(_openingState, IsOpened ? 1 : 0, Time.deltaTime * _openingSpeed);

        _doorMaterial.SetInt("_MirrorLeftSide", _useExitDoorTexture ? 0 : 1);
        _doorMaterial.SetFloat("_OpeningState", _openingCurve.Evaluate(_openingState));
    }
}
