using System.Collections;
using UnityEngine;

public class Passage : MonoBehaviour
{
    [Header("Passage Parameters")]
    [SerializeField] private Transform _alignTransform;
    public Transform Alignment => _alignTransform;

    [SerializeField] private bool _opened;
    public bool IsOpened => _opened;

    public float WalkInTime;
    public float WalkOutTime;
    

    [Header("Passage Targets")]
    public Passage TargetPassage;
    public string TargetScene;

    [Header("Sound Effects")]
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private AudioClip _closeSound;
    [SerializeField] private AudioClip _passageSound;

    public bool CanPassThrough()
    {
        return _opened && (TargetPassage != null || TargetScene != "");
    }

    public void Open()
    {
        _opened = true;
        _audioManager.PlayClip(_openSound);
    }

    public void Close()
    {
        _opened = false;
        _audioManager.PlayClip(_closeSound);
    }
}
