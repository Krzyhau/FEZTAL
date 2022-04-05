using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LaserReceiver : MonoBehaviour
{
    public UnityEvent onPowered;
    public UnityEvent onUnpowered;
    public bool powered = false;
    
    int poweredCounter = 0;
    bool prevPowered = false;

    void Update()
    {
        if (powered != prevPowered) {
            if (powered) onPowered.Invoke();
            else onUnpowered.Invoke();

            prevPowered = powered;
        }
    }

    public void LaserPower() {
        powered = true;
        poweredCounter++;
    }

    public void LaserUnpower() {
        poweredCounter = Mathf.Max(poweredCounter - 1, 0);
        if (poweredCounter == 0) powered = false;
    }
}
