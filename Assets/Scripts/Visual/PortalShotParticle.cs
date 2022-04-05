using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalShotParticle : MonoBehaviour
{
    public float timespan = 1;
    public Vector3 startPosition;
    public Vector3 targetPosition;
    public Color color;

    LineRenderer line;
    float passedTime = 0;

    private void Start() {
        Destroy(gameObject, timespan);
        line = GetComponent<LineRenderer>();

        var materials = line.materials;

        RefreshLinePos();

        for (int i = 0; i < materials.Length; i++) {
            materials[i].SetColor("MainColor", color);
            materials[i].SetFloat("LineOffset", Random.Range(0.0f,10.0f));
            materials[i].SetFloat("LineScale", transform.localScale.x);
        }

        line.materials = materials;

        
    }

    void Update()
    {
        passedTime += Time.deltaTime;

        RefreshLinePos();

        var materials = line.materials;

        for(int i = 0; i < materials.Length; i++) {
            materials[i].SetFloat("HideProgress", passedTime/timespan);
        }

        line.materials = materials;
    }

    void RefreshLinePos() {
        transform.position = startPosition;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 projTarget = targetPosition - camForward * Vector3.Dot(targetPosition - startPosition, camForward);

        Vector3 lenghtVector = projTarget - startPosition;

        transform.localScale = new Vector3(lenghtVector.magnitude, 0, 0);

        Vector3 upVector = Vector3.Cross(camForward, lenghtVector.normalized);
        transform.rotation = Quaternion.LookRotation(camForward, upVector);
    }
}
