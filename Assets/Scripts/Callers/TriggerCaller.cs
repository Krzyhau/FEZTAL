using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerCaller : MonoBehaviour
{
    public bool onlyOnce = false;
    public bool boxCaster = false;
    public LayerMask objectMask;
    public UnityEvent onStartTouch;
    public UnityEvent onEndTouch;


    bool triggeredEnterAlready = false;
    bool triggeredExitAlready = false;

    BoxCollider col;
    private void Start() {
        col = GetComponent<BoxCollider>();
    }

    private void Update() {
        if (boxCaster && !triggeredEnterAlready) {
            if (Physics.CheckBox(transform.position + col.center, col.size * 0.5f, Quaternion.identity, objectMask)) {
                onStartTouch.Invoke();
                triggeredEnterAlready = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (onlyOnce && triggeredEnterAlready) return;

        if(objectMask == (objectMask | (1 << other.gameObject.layer))) {
            onStartTouch.Invoke();
            triggeredEnterAlready = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (onlyOnce && triggeredExitAlready) return;

        if (objectMask == (objectMask | (1 << other.gameObject.layer))) {
            onEndTouch.Invoke();
            triggeredExitAlready = true;
        }
    }
}
