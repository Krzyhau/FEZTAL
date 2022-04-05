using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAligner : MonoBehaviour
{
    void LateUpdate() {
        transform.forward = Camera.main.transform.forward;
    }

    public void SetAngle(float angle) {
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }
}
