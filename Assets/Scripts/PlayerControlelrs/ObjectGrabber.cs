using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGrabber : MonoBehaviour
{
    [SerializeField] private BoxCollider _collider;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private GomezController _playerController;

    [Header("Parameters")]
    [SerializeField] private float _maxGrabbedDistance;
    [SerializeField] private float _maxGrabbedSpeed;
    [SerializeField] private float _holdDistance;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask _grabMask;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip _dropSound;
    [SerializeField] private AudioClip _successPickSound;
    [SerializeField] private AudioClip _failedPickSound;


    private GameObject _grabbedEntity = null;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            AttemptGrab();
        }
    }

    private void FixedUpdate()
    {
        UpdateGrabbedEntity();
    }

    private void UpdateGrabbedEntity()
    {
        if (!_grabbedEntity) return;

        _grabbedEntity.transform.position = Vector3.MoveTowards(_grabbedEntity.transform.position, GetHeldPosition(), _maxGrabbedSpeed * Time.fixedDeltaTime);
        _grabbedEntity.transform.rotation = Quaternion.RotateTowards(_grabbedEntity.transform.rotation, GetHeldRotation(), _maxGrabbedSpeed * 20.0f * Time.fixedDeltaTime);

        Vector3 eulerRot = _grabbedEntity.transform.localEulerAngles;
        _grabbedEntity.transform.localEulerAngles = new Vector3(0, eulerRot.y, 0);
    }

    public void AttemptGrab()
    {
        if (_grabbedEntity)
        {
            _grabbedEntity.transform.position = GetHeldPosition();
            _grabbedEntity.layer = LayerMask.NameToLayer("Grabbable");
            Physics.IgnoreCollision(_grabbedEntity.GetComponent<Collider>(), _collider, false);
            _grabbedEntity = null;
            _audioManager.PlayClip(_dropSound);
            return;
        }

        Vector3 clickPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Physics.Raycast(clickPoint, Camera.main.transform.forward, out var hit, 128.0f, _groundMask))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grabbable"))
            {
                // attempting to grab an object. check if 2d distance is shorter than max grab distance
                Vector3 dist = hit.collider.gameObject.transform.position - transform.position;
                dist -= Camera.main.transform.forward * Vector3.Dot(Camera.main.transform.forward, dist);
                if (dist.magnitude < _maxGrabbedDistance)
                {
                    _grabbedEntity = hit.collider.gameObject;
                    hit.collider.gameObject.layer = LayerMask.NameToLayer("Held");
                    Physics.IgnoreCollision(_grabbedEntity.GetComponent<Collider>(), _collider, true);
                    _playerController.GetComponent<Animator>().Play("gomez_shoot", 0, 0.0f);
                    _playerController.FlipSprite(Vector3.Dot(Camera.main.transform.right, hit.collider.transform.position - transform.position) < 0);
                    _audioManager.PlayClip(_successPickSound);
                }
            }
        }
        if (!_grabbedEntity)
        {
            _audioManager.PlayClip(_failedPickSound);
        }
    }


    private Vector3 GetHeldPosition()
    {
        Vector3 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 targetPos = transform.position;

        Vector3 lookDir = mousePoint - targetPos;
        lookDir -= Camera.main.transform.forward * Vector3.Dot(Camera.main.transform.forward, lookDir);
        lookDir = lookDir.normalized * _holdDistance;
        targetPos += lookDir;

        if (Physics.Raycast(transform.position, lookDir.normalized, out var hit, lookDir.magnitude + 1.0f, _groundMask))
        {
            targetPos = transform.position + Vector3.up * _collider.size.y;
        }

        return targetPos;
    }

    private Quaternion GetHeldRotation()
    {
        Vector3 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 lookPoint;
        if (Physics.Raycast(mousePoint, Camera.main.transform.forward, out var mouseHit, 128.0f, _groundMask))
        {
            lookPoint = mouseHit.point;
        } else
        {
            lookPoint = transform.position + (mousePoint - Camera.main.transform.position);
        }

        lookPoint -= Vector3.up * Vector3.Dot(Vector3.up, lookPoint - _grabbedEntity.transform.position);

        Quaternion targetRot = Quaternion.LookRotation(lookPoint - _grabbedEntity.transform.position, Vector3.up);
        return targetRot;
    }
}
