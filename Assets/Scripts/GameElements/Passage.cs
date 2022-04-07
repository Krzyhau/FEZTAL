using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("Sound Effects")]
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private AudioClip _closeSound;
    [SerializeField] private AudioClip _passageSound;

    [Header("Events")]
    [SerializeField] private UnityEvent _onPassageEntry;
    [SerializeField] private UnityEvent _onPassage;
    [SerializeField] private UnityEvent _onPassageExit;

    public UnityEvent OnPassageEntry => _onPassageEntry;
    public UnityEvent OnPassage => _onPassage;
    public UnityEvent OnPassageExit => _onPassageExit;

    public bool CanPassThrough()
    {
        return _opened;
    }

    public void Open()
    {
        if(!_opened)_audioManager.PlayClip(_openSound);
        _opened = true;
        
    }

    public void Close()
    {
        if(_opened) _audioManager.PlayClip(_closeSound);
        _opened = false;
    }
}
