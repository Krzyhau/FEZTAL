using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AutoCaller : MonoBehaviour
{
    public UnityEvent OnSceneStart;
    private void Start() => OnSceneStart?.Invoke();
}
