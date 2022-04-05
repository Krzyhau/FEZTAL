using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePhysics : MonoBehaviour
{
    public float friction = 5f;
    public LayerMask groundMask;


    BoxCollider col;
    Rigidbody rigid;

    bool grounded = true;

    void Start() {
        col = GetComponent<BoxCollider>();
        rigid = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // check grounded state
        grounded = Physics.CheckBox(
            transform.position - new Vector3(0, col.size.y * 0.502f, 0),
            new Vector3(col.size.x * 0.499f, 0.009f, col.size.z * 0.499f),
            Quaternion.identity,
            groundMask
        );

        Vector3 vel = rigid.velocity;

        if (gameObject.layer == LayerMask.NameToLayer("Held")) {
            vel = Vector3.zero;
        } if (grounded) {
            vel.y = 0;
            vel = Vector3.MoveTowards(vel, Vector3.zero, friction * Time.fixedDeltaTime);
        } else {
            vel.y += Physics.gravity.y;
        }

        rigid.velocity = vel;
    }
}
