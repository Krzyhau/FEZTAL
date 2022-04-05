using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignScript : MonoBehaviour
{
    public int displayedNumber = 0;

    public bool displayOnStart = false;

    MeshRenderer rend;

    void Start()
    {
        rend = GetComponent<MeshRenderer>();
        rend.material.SetFloat("Number", displayedNumber);
        if (displayOnStart) {
            StartCoroutine("DelayedDisplaySequence");
        }
    }

    
    public void TurnOn() {
        StartCoroutine("DisplaySequence");
    }

    IEnumerator DelayedDisplaySequence() {
        yield return new WaitForSeconds(0.4f);
        TurnOn();
    }

    IEnumerator DisplaySequence() {
        rend.material.SetInt("Backlit", 1);
        yield return new WaitForSeconds(0.1f);
        rend.material.SetInt("Backlit", 0);
        yield return new WaitForSeconds(0.2f);
        rend.material.SetInt("Backlit", 1);
        yield return new WaitForSeconds(0.1f);
        rend.material.SetInt("DisplayNumber", 1);
    }

    public void TurnOff() {
        rend.material.SetInt("Backlit", 0);
        rend.material.SetInt("DisplayNumber", 0);
    }

}
