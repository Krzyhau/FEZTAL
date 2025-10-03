using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum ShiftDirection
{
    NONE,
    LEFT,
    RIGHT
}

public class FEZCameraController: MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Camera _camera;
    [SerializeField] private AudioManager _audioManager;

    [Header("Parameters")]
    [SerializeField] private float _rotationInterpolation;
    [SerializeField] private float _blockTime;
    [SerializeField] private float _followSpeed;
    [SerializeField] private float _maxHorizontalDistance;
    [SerializeField] private float _verticalOffset;
    [SerializeField] private AnimationCurve _rotationMovement;
    [SerializeField] private float _aspectRatio;
    [SerializeField] private float _size;
    [SerializeField] private float _shiftRegainControlTime = 0.1f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip _shiftLeftSound;
    [SerializeField] private AudioClip _shiftRightSound;

    private Vector3 _lastFollowPoint = Vector3.zero;
    private List<Transform> _followTargets = new List<Transform>();
    private float _prevLookAng = 0;
    private float _lookAng = 0;
    private float _blockTimer = 0;

    private bool _positionForcedThisFrame = false;


    [HideInInspector] public bool ControlEnabled = true;
    public Camera Camera => _camera;
    public float PhysicsAngle => _lookAng;
    public float VisualAngle => transform.eulerAngles.y;
    public bool PositionForcedThisFrame => _positionForcedThisFrame;

    private void Update()
    {
        if (_positionForcedThisFrame)
        {
            _positionForcedThisFrame = false;
            return;
        }

        HandleInputs();

        UpdateCameraPosition();
    }


    private void HandleInputs()
    {
        // camera perspective rotation
        if (!CanShift()) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Shift(ShiftDirection.LEFT);
            SpeedrunValues.shiftCount++;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Shift(ShiftDirection.RIGHT);
            SpeedrunValues.shiftCount++;
        }
    }

    void UpdateCameraPosition()
    {
        Vector3 followPoint = GetActualFollowPoint();

        // updating the position
        if (transform.parent)
        {
            transform.parent = null;
            transform.position = followPoint;
        } else
        {
            transform.position = Vector3.Lerp(transform.position, followPoint, _followSpeed * Time.deltaTime);
        }

        // updating the rotation.
        float ang = _lookAng;
        if (_blockTimer > 0)
        {
            float t = _rotationMovement.Evaluate(1 - (_blockTimer / _blockTime));
            ang = Mathf.LerpAngle(_prevLookAng, _lookAng, t);
        }
        transform.rotation = Quaternion.Euler(0, ang, 0);

        _camera.orthographicSize = GetActualSize();

        _blockTimer = Mathf.Max(0, _blockTimer - Mathf.Min(Time.deltaTime, 0.1f));
    }
    
    public void SnapToFollowPoint()
    {
        transform.position = GetActualFollowPoint();
    }

    // find the follow point camera should follow right now
    public Vector3 GetFollowPoint()
    {
        if(_followTargets.Count > 0)
        {
            _lastFollowPoint = _followTargets[_followTargets.Count - 1].position;
        }
        return _lastFollowPoint;
    }

    // calculating actual follow point (it has certain horizontal distance it can keep without following)
    public Vector3 GetActualFollowPoint()
    {
        Vector3 followPoint = GetFollowPoint() + Vector3.up * _verticalOffset;

        float hVel = Vector3.Dot(transform.right, followPoint - transform.position);
        float hDVel = 0;
        if (hVel != 0) hDVel = (hVel / Mathf.Abs(hVel)) * Mathf.Max(0, Mathf.Abs(hVel) - _maxHorizontalDistance);

        followPoint += transform.right * (hDVel - hVel);

        return followPoint;
    }

    public void AddFollowTarget(Transform follow)
    {
        if (!_followTargets.Contains(follow)) _followTargets.Add(follow);
    }

    public void RemoveFollowTarget(Transform follow)
    {
        if (_followTargets.Contains(follow)) _followTargets.Remove(follow);
    }

    public void PrioritizeFollowTarget(Transform follow)
    {
        if (_followTargets.Contains(follow))
        {
            _followTargets.Remove(follow);
            _followTargets.Add(follow);
        }
    }


    // starts shifting perspective in given direction
    public void Shift(ShiftDirection dir)
    {
        _prevLookAng = transform.eulerAngles.y;
        float ang = 0;
        switch (dir)
        {
            case ShiftDirection.LEFT: ang = 90.0f; break;
            case ShiftDirection.RIGHT: ang = -90.0f; break;
        }

        _lookAng = (_lookAng + ang) % 360.0f;
        _blockTimer = _blockTime;

        if (ang > 0) _audioManager.PlayClip(_shiftLeftSound);
        if (ang < 0) _audioManager.PlayClip(_shiftRightSound);
    }

    public void SetAngle(float angle)
    {
        _lookAng = angle;
        _prevLookAng = _lookAng;
    }

    public bool IsShifting()
    {
        return _blockTimer >= _shiftRegainControlTime;
    }

    public bool CanShift()
    {
        return !(_blockTimer > 0 && _lookAng == _prevLookAng) && ControlEnabled;
    }

    // updating camera size to not go above given aspect ratio (avoids seeing unwanted surfaces on high width and low height)
    public float GetActualSize()
    {
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float camSize = _size;
        if (screenRatio > _aspectRatio)
        {
            camSize = camSize * (_aspectRatio / screenRatio);
        }
        return camSize;
    }

    // overrides camera position this frame. It can be used by scripted animations
    public void SetPositionThisFrame(Vector3 pos, float size = -1, float angle = -999)
    {
        transform.position = pos;
        if (size > 0) _camera.orthographicSize = size;
        if (angle != -999) transform.rotation = Quaternion.Euler(0, angle, 0);

        _positionForcedThisFrame = true;
    }
}
