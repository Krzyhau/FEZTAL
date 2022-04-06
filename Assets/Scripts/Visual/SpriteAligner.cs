using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAligner : MonoBehaviour
{
    public bool UsePhysicsAngle = false;
    public bool Mirrored = false;
    void LateUpdate() {
        float ang = UsePhysicsAngle ? LevelManager.Camera.PhysicsAngle : LevelManager.Camera.VisualAngle;
        transform.rotation = Quaternion.Euler(0, ang, 0);

        transform.localScale = new Vector3(Mirrored ? -1 : 1,1,1);
    }

    public void SetAngle(float angle) {
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }
}
