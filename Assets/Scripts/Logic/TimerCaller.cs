using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimerCaller : MonoBehaviour
{
    public float waitTime;
    public UnityEvent onTimePassed;

    public void Call() {
        StartCoroutine("DelayedCall");
    }

    IEnumerator DelayedCall() {
        yield return new WaitForSeconds(waitTime);
        onTimePassed.Invoke();
    }
}
