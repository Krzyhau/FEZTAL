using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CounterCaller : MonoBehaviour
{
    public int value = 0;
    public int max = 3;
    public int min = 0;

    public UnityEvent onReachedMax;
    public UnityEvent onChangedFromMax;
    public UnityEvent onReachedMin;
    public UnityEvent onChangedFromMin;

    int oldValue = 0;

    void Start()
    {
        value = Mathf.Clamp(value, min, max);
        oldValue = value;
    }

    
    void Update()
    {
        value = Mathf.Clamp(value, min, max);
        if (oldValue != value) {
            if (value == max) onReachedMax.Invoke();
            if (value == min) onReachedMin.Invoke();
            if (oldValue == max) onChangedFromMax.Invoke();
            if (oldValue == min) onChangedFromMin.Invoke();

            oldValue = value;
        }
    }

    public void Add(int i) {
        value += i;
    }

    public void Subtract(int i) {
        value -= i;
    }
}
