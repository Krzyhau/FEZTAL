using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CakeMovement : MonoBehaviour
{
    public float wobbleSpeed = 0.1f;
    public float wobbleSize = 0.1f;
    public float yOffset = 1.0f;
    public float rotationSpeed = 10f;

    Vector3 startPos;
    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.unscaledDeltaTime);
        transform.position = startPos + Vector3.up * (yOffset + Mathf.Sin(Time.unscaledTime * wobbleSpeed * Mathf.PI) * wobbleSize);
    }
}
