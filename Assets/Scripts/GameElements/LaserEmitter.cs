using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserEmitter : MonoBehaviour
{
    public bool emitting = false;
    public bool isRelay = false;
    public GameObject laserPrefab;
    public Transform emitPoint;
    public GameObject visualElement;

    bool oldEmittingState = false;
    GameObject laser;
    int relays = 0;

    void Start()
    {
        if (!emitPoint) emitPoint = transform;
        UpdateLaserState(true);
    }


    void Update()
    {
        if (oldEmittingState != emitting) {
            UpdateLaserState();
        }
    }

    public void UpdateLaserState(bool force = false) {
        if(oldEmittingState != emitting || force) {

            if (emitting) {
                laser = Instantiate(laserPrefab, emitPoint.position, emitPoint.rotation);
                laser.transform.parent = transform;
            } else if(laser) {
                Destroy(laser);
                laser = null;
            }

            if(visualElement)visualElement.SetActive(emitting);
            oldEmittingState = emitting;
        }
    }

    public void AddRelaySource() {
        relays++;
        emitting = true;
        UpdateLaserState();
    }

    public void RemoveRelaySource() {
        relays = Mathf.Max(relays - 1, 0);
        if (relays == 0) emitting = false;
        UpdateLaserState();
    }
}
