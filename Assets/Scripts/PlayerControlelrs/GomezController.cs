using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GomezController : MonoBehaviour
{
    [SerializeField] private FEZCameraController _cameraController;
    [SerializeField] private Passage _startPassage;

    // movement variables
    [Header("Movement Variables")]
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _maxAirSpeed;
    [SerializeField] private float _groundAccel;
    [SerializeField] private float _airAccel;
    [SerializeField] private float _groundFriction;
    [SerializeField] private float _airFriction;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpHoldMultiplier;
    [SerializeField] private float _maxGrabbedSpeed;
    [SerializeField] private float _maxGrabbedDistance;
    [SerializeField] private float _holdDistance;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask _passageMask;
    [SerializeField] private LayerMask _grabMask;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip _failedPickSound;
    [SerializeField] private AudioClip _successPickSound;
    [SerializeField] private AudioClip _dropSound;
    [SerializeField] private AudioClip _portalPassageSound;

    private BoxCollider _collider;
    private Rigidbody _rigidbody;
    private Animator _animator;
    private SpriteAligner _spriteAligner;
    private AudioManager _audioManager;

    private GameObject _grabbedEntity = null;

    private bool _grounded = true;

    private float _wishDir = 0;
    private int _jumpState = 0;
    private float _prevVelY = 0;
    private Vector3 _lastGroundPos;

    // passage handling
    private Passage _passage = null;
    private float _passageTime = 0.0f;
    private bool _passageEntering = false;

    private bool _blockMovement = false;
    private bool _freezed = false;

    public FEZCameraController CameraController => _cameraController;
    public bool Grounded => _grounded;
    public bool IsPassing => _passage != null;

    void Start()
    {
        _collider = GetComponent<BoxCollider>();
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _spriteAligner = transform.Find("Sprite").GetComponent<SpriteAligner>();
        _audioManager = GetComponent<AudioManager>();

        if (_startPassage)
        {
            UsePassage(_startPassage, true);
        }

        _lastGroundPos = transform.position;
        _cameraController.AddFollowTarget(transform);
    }

    private void FixedUpdate()
    {
        // check if we're standing on ground before doing anything
        CheckGround();

        // deal with passing through passages
        UpdatePassageInteraction();

        // player movement
        if (CanMove() && !_freezed)
        {
            Vector3 vel = _rigidbody.velocity;

            // bringing back previous y velocity
            // we reset x velocity, so that doesn't need to be saved
            if (_prevVelY != 0)
            {
                vel.y = _prevVelY;
                _prevVelY = 0;
            }

            // jumping
            if (_grounded && _jumpState == 1)
            {
                vel.y = _jumpForce;
                _grounded = false;
                _jumpState = 2;
            }
            if (vel.y < 0)
            {
                _jumpState = 0;
            }

            // gravity
            if (!_grounded)
            {
                vel.y += Physics.gravity.y * (_jumpState == 2 ? _jumpHoldMultiplier : 1);
            }

            // horizontal movement
            Vector3 moveDir = Quaternion.Euler(0, _cameraController.PhysicsAngle + 90, 0) * Vector3.forward;
            float curSpeed = Vector3.Dot(_rigidbody.velocity, moveDir);

            curSpeed += (_grounded ? _groundAccel : _airAccel) * _wishDir;
            float maxCurSpeed = (_grounded ? _maxSpeed : _maxAirSpeed);
            if (Mathf.Abs(curSpeed) > maxCurSpeed)
            {
                curSpeed = curSpeed / Mathf.Abs(curSpeed) * maxCurSpeed;
            }
            if (_wishDir == 0)
            {
                curSpeed *= 1 - (_grounded ? _groundFriction : _airFriction);
            }
            vel = moveDir * curSpeed + Vector3.up * vel.y;

            _rigidbody.velocity = vel;


            Vector3 castDir = Quaternion.Euler(0, -90, 0) * moveDir;

            Handle2DLanding(castDir);
            Handle2DLanding(castDir * -1);


            for (int i = -1; i <= 1; i++)
            {
                // handle wall movement for forward faces.
                HandleWallMovement(castDir, i);
                // doing the same for back walls, except check for blocked flat jump (indoors movement)
                HandleWallMovement(-castDir, i, false);
            }

        }
        else
        {
            if (_prevVelY == 0 && _rigidbody.velocity.y != 0)
            {
                _prevVelY = _rigidbody.velocity.y;
            }
            _rigidbody.velocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (LevelManager.GetInstance().IsPaused()) return;


        if (!_blockMovement)
        {
            _wishDir = 0;
            if (Input.GetKey(KeyCode.A)) _wishDir -= 1;
            if (Input.GetKey(KeyCode.D)) _wishDir += 1;


            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_jumpState == 0) _jumpState = 1;
            } else if (!Input.GetKey(KeyCode.Space))
            {
                _jumpState = 0;
            }

            if (Input.GetKeyDown(KeyCode.W) && CanControl())
            {
                AttemptPassage();
            }
        }

        if (CanMove() || IsPassing)
        {
            _freezed = false;
        } else
        {
            _freezed = true;
        }

        UpdateGrabbedObject();

        UpdateAnimator();
    }

    // updates ground flag
    private void CheckGround()
    {
        _grounded = Physics.CheckBox(
            transform.position - new Vector3(0, _collider.size.y * 0.5f, 0),
            new Vector3(_collider.size.x * 0.499f, 0.01f, _collider.size.z * 0.499f),
            Quaternion.identity,
            _groundMask
        );

        // additional check for safe spot for respawning.
        // Why am I doing this differently than normal ground check?
        // Glad you asked. I have absolutely no idea!!!!
        if (CanMove() && !_freezed)
        {
            bool safeGround = Physics.Raycast(
                transform.position - new Vector3(0, _collider.size.y * 0.49f, 0),
                Vector3.down, 0.02f, _groundMask
            );
            if (safeGround)
            {
                _lastGroundPos = transform.position;
            }
        }
    }

    // handle 2D movement "through" walls
    bool HandleWallMovement(Vector3 castDir, int vOffset, bool allowFlatJump = true)
    {
        // doing two raycasts: one to detect if we're not already obscured by a wall
        // if we do, doing another one to check if we will be obscured by a wall
        float hitboxWidth = _collider.size.x;
        const float castDist = 64f;
        Vector3 startPos = transform.position - castDir * castDist + Vector3.up * vOffset * _collider.size.y * 0.49f;
        if (vOffset == 0) startPos += Vector3.down * 0.005f;
        RaycastHit hit;
        for (int i = 0; i < 2; i++)
        {
            float castLength = castDist + hitboxWidth * 0.4999f;
            if (i == 1)
            {
                Vector3 moveVel = _rigidbody.velocity;
                moveVel.y = Mathf.Max(moveVel.y, 0);
                Vector3 horVer = new Vector3(moveVel.x, 0, moveVel.z);
                if (horVer.magnitude != 0) startPos += horVer.normalized * hitboxWidth * 0.5f;
                startPos += moveVel * Time.fixedDeltaTime;
                castLength = castDist + hitboxWidth * 0.5001f;
            }
            RaycastHit[] hits = Physics.RaycastAll(startPos, castDir, castLength, _groundMask, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                hit = hits[0];
                foreach (RaycastHit h in hits)
                {
                    if (h.distance > hit.distance) hit = h;
                }
                float hitForce = -Vector3.Dot(hit.normal, castDir);
                if (hitForce > 0.01)
                {
                    if (i == 0)
                    {
                        Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red);
                        return false;
                    } 
                    else
                    {
                        float spaceBetweenWall = 1.1f;
                        float flatJumpForce = (castDist - hit.distance + hitboxWidth * 0.5f * (spaceBetweenWall + (1f - hitForce) * 2.0f));
                        // additional raycast to check if illegal jump occurs
                        if (!allowFlatJump)
                        {
                            RaycastHit flatHit;
                            Vector3 flatStartPos = startPos + castDir * (castDist - hitboxWidth * 0.5f);
                            bool blockedFlatJump = Physics.Raycast(flatStartPos, -castDir, out flatHit, flatJumpForce, _groundMask, QueryTriggerInteraction.Ignore);
                            if (blockedFlatJump)
                            {
                                Debug.DrawLine(hit.point, hit.point + hit.normal, new Color(1.0f, 0.5f, 0.0f));
                                Debug.DrawLine(flatHit.point, flatHit.point + flatHit.normal, new Color(1.0f, 0.5f, 0.5f));
                                return false;
                            }
                        }
                        //block jump if there is no space
                        RaycastHit spaceHit;
                        bool blockedSpace = Physics.Raycast(hit.point, hit.normal, out spaceHit, hitboxWidth, _groundMask, QueryTriggerInteraction.Ignore);
                        if (blockedSpace)
                        {
                            Debug.DrawLine(hit.point, spaceHit.point, new Color(0.5f, 0f, 0f));
                            return false;
                        }

                        Debug.DrawLine(hit.point, hit.point + hit.normal, Color.green);
                        transform.position -= castDir * flatJumpForce;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    void Handle2DLanding(Vector3 castDir)
    {
        if (_grounded || _rigidbody.velocity.y > 0) return;

        //check if we're not about to land on a ground
        bool aboutToLand = Physics.CheckBox(
            transform.position - new Vector3(0, _collider.size.y * 0.5f, 0),
            new Vector3(_collider.size.x * 0.499f, Mathf.Abs(_rigidbody.velocity.y) * Time.fixedDeltaTime * 2.1f, _collider.size.z * 0.499f),
            Quaternion.identity,
            _groundMask
        );

        if (aboutToLand) return;


        // now we can actually do the landing logic
        const float castDist = 64f;

        Vector3 startPos = transform.position + Vector3.down * _collider.size.y * 0.5f;
        Vector3 startPosAfterMove = startPos + _rigidbody.velocity * Time.fixedDeltaTime;

        Vector3 sideVec = Vector3.Cross(Vector3.up, castDir.normalized);
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 o = sideVec * i * _collider.size.x * 0.49f;
            bool foundGround = Physics.Raycast(startPosAfterMove + o, castDir, out var groundHit, castDist, _groundMask, QueryTriggerInteraction.Ignore);
            if (foundGround)
            {
                bool foundFar = Physics.BoxCast(
                    startPos + Vector3.up * 0.1f,
                    new Vector3(_collider.size.x, 0.1f, 0.1f),
                    castDir, out var farHit, Camera.main.transform.rotation,
                    castDist, _groundMask, QueryTriggerInteraction.Ignore
                );
                if (!foundFar || farHit.distance - _collider.size.x > groundHit.distance)
                {
                    transform.position = transform.position + castDir.normalized * (groundHit.distance + _collider.size.x * 0.51f);
                    Debug.DrawLine(groundHit.point, groundHit.point + groundHit.normal, Color.cyan);
                    Debug.DrawLine(groundHit.point, startPos + o, Color.cyan);
                    return;
                }
            }
        }
    }
    public void AttemptPassage()
    {
        if (!_grounded) return;

        bool foundPassage = Physics.Raycast(transform.position, Camera.main.transform.forward, out var hit, 128.0f, _passageMask);
        if (foundPassage)
        {
            var obj = hit.collider.gameObject;
            var passage = obj.GetComponent<Passage>();
            if (passage && passage.CanPassThrough() && Vector3.Dot(Camera.main.transform.forward, -hit.normal) > 0.9)
            {
                UsePassage(passage);
            }
        }
    }

    private void UpdatePassageInteraction()
    {
        if (_passage == null) return;

        var desiredPassageTime = (_passageEntering) ? _passage.WalkInTime : _passage.WalkOutTime;

        var passageFactor = _passageTime / desiredPassageTime;
        if (_passageEntering) passageFactor = 1 - passageFactor;

        var walkInOffset = 0.5f * _collider.size.x * passageFactor;

        // slowly adjust the player position so it gets closer to the passage's align position
        Vector3 pos;
        if (_passageEntering)
        {
            pos = transform.position;
            var offsetForce = Vector3.Dot(_passage.Alignment.forward, pos - _passage.Alignment.position) - walkInOffset;
            pos -= _passage.Alignment.forward * offsetForce;
        } 
        else
        {
            pos = _passage.Alignment.position + _passage.Alignment.forward * walkInOffset;
        }
        
        Vector3 desiredPos = _passage.Alignment.position + _passage.Alignment.forward * walkInOffset;
        transform.position = Vector3.MoveTowards(pos, desiredPos, _maxSpeed * Time.fixedDeltaTime * 0.25f);

        // update some parameters
        _spriteAligner.UsePhysicsAngle = true;
        _cameraController.ControlEnabled = false;
        _collider.enabled = false;
        _rigidbody.velocity = Vector3.zero;

        _passageTime += Time.fixedDeltaTime;
        // passage ends here
        if(_passageTime >= desiredPassageTime)
        {
            _collider.enabled = true;
            _spriteAligner.UsePhysicsAngle = false;
            _cameraController.ControlEnabled = true;

            // move the player to another passage if current one leads to it
            if (_passageEntering && _passage.TargetPassage)
            {
                UsePassage(_passage.TargetPassage, true);
            } 
            else
            {
                if (_passage == _startPassage) _passage.Close();

                _passageTime = 0;
                _passage = null;
            }
        }
    }

    private void UsePassage(Passage passage, bool exit=false)
    {
        _passage = passage;
        _passageTime = 0.0f;
        _passageEntering = !exit;

        if (exit)
        {
            _animator.Play("gomez_walkout",0, 0.8f - _passage.WalkOutTime);
        } 
        else
        {
            _animator.Play("gomez_walkin");
        }

        // rotate camera so it enters/exits the passage away from/towards it
        float ang = Vector3.SignedAngle(_cameraController.transform.forward, -passage.transform.forward, Vector3.up);
        int rotations = (int)Mathf.Floor((Mathf.Abs(ang) + 45) / 90);
        for (int i = 0; i < rotations; i++)
        {
            _cameraController.Shift(ang > 0 ? ShiftDirection.LEFT : ShiftDirection.RIGHT);
        }

        // check if the door is transition door
        if(!exit && passage.TargetScene.Length > 0)
        {
            LevelManager.TransitionToLevel(passage.TargetScene);
        }
    }

    void UpdateGrabbedObject()
    {
        if (_grabbedEntity)
        {
            Vector3 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 targetPos = transform.position;

            Vector3 lookDir = mousePoint - targetPos;
            lookDir -= Camera.main.transform.forward * Vector3.Dot(Camera.main.transform.forward, lookDir);
            lookDir = lookDir.normalized * _holdDistance;
            targetPos += lookDir;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, lookDir.normalized, out hit, lookDir.magnitude + 1.0f, _groundMask))
            {
                targetPos = transform.position + Vector3.up * _collider.size.y;
            }

            RaycastHit mouseHit;
            Vector3 lookPoint;
            if (Physics.Raycast(mousePoint, Camera.main.transform.forward, out mouseHit, 128.0f, _groundMask))
            {
                lookPoint = mouseHit.point;
            } 
            else
            {
                lookPoint = transform.position + (mousePoint - Camera.main.transform.position);
            }

            lookPoint -= Vector3.up * Vector3.Dot(Vector3.up, lookPoint - _grabbedEntity.transform.position);

            _grabbedEntity.transform.position = Vector3.MoveTowards(_grabbedEntity.transform.position, targetPos, _maxGrabbedSpeed * Time.deltaTime);
            Quaternion targetRot = Quaternion.LookRotation(lookPoint - _grabbedEntity.transform.position, Vector3.up);
            _grabbedEntity.transform.rotation = Quaternion.RotateTowards(_grabbedEntity.transform.rotation, targetRot, _maxGrabbedSpeed * 20.0f * Time.deltaTime);

            Vector3 eulerRot = _grabbedEntity.transform.localEulerAngles;
            _grabbedEntity.transform.localEulerAngles = new Vector3(0, eulerRot.y, 0);

            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                _grabbedEntity.transform.position = targetPos;
                _grabbedEntity.layer = LayerMask.NameToLayer("Grabbable");
                Physics.IgnoreCollision(_grabbedEntity.GetComponent<Collider>(), _collider, false);
                _grabbedEntity = null;
                _audioManager.PlayClip(_dropSound);
            }
        } 
        else if (CanControl())
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
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
                            _animator.Play("gomez_shoot", 0, 0.0f);
                            FlipSprite(Vector3.Dot(Camera.main.transform.right, hit.collider.transform.position - transform.position) < 0);
                            _audioManager.PlayClip(_successPickSound);
                        }
                    }
                }
                if (!_grabbedEntity)
                {
                    _audioManager.PlayClip(_failedPickSound);
                }
            }
        }
    }


    void UpdateAnimator()
    {
        if (CanMove() || IsPassing)
        {
            _animator.SetBool("Grounded", _grounded);

            Vector3 vel = _rigidbody.velocity;

            float jumpState = Mathf.Clamp(-vel.y / (_jumpForce * 2) + 0.5f, 0, 1);
            _animator.SetFloat("FlyState", jumpState);


            vel.y = 0;
            float horizontalVel = vel.magnitude;

            _animator.SetBool("Walking", horizontalVel > 0.1f);
            _animator.SetBool("Running", horizontalVel > _maxSpeed * 0.6f);

            _animator.SetInteger("Timer", (_animator.GetInteger("Timer") + 1) % 1000);
            if (Random.Range(0, 1000) < 2) _animator.SetTrigger("Blink");

            float moveDir = Vector3.Dot(vel, _cameraController.Camera.transform.right);

            int flipDir = 0;
            int edgeFlipDir = 0;

            if (_wishDir > 0)
            {
                flipDir = 1;
            } else if (_wishDir < 0)
            {
                flipDir = -1;
            }

            bool standingOnEdge = false;
            if (_grounded && _rigidbody.velocity.magnitude < 0.01f)
            {
                Vector3 startPos = transform.position + Vector3.down * _collider.size.y * 0.49f;
                Vector3 edgeOffset = Camera.main.transform.right * _collider.size.x * 0.5f;
                if (!Physics.Raycast(startPos, Vector3.down, 0.1f, _groundMask))
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        bool hasEdge = Physics.Raycast(startPos + edgeOffset * i, Vector3.down, 0.1f, _groundMask);
                        if (!hasEdge)
                        {
                            standingOnEdge = !standingOnEdge; // prevents activating the animation when standing "midair"
                            edgeFlipDir = i;
                        }
                    }
                }
            }

            if (standingOnEdge) flipDir = edgeFlipDir;
            if (flipDir != 0) FlipSprite(flipDir < 0 ? true : false);

            _animator.SetBool("OnEdge", standingOnEdge);

            // special cases

            if (moveDir * _wishDir < -0.1 && _grounded)
            {
                _animator.Play("gomez_drift");
            }

            _animator.speed = 1;
        } else
        {
            _animator.speed = 0;
        }
    }

    public void FlipSprite(bool flipped)
    {
        _spriteAligner.Mirrored = flipped;
    }

    public void BlockMovement(bool block)
    {
        _blockMovement = block;
        _cameraController.ControlEnabled = !block;
    }

    public bool CanMove()
    {
        return !_cameraController.IsShifting() && !IsPassing;
    }

    public bool CanControl()
    {
        return CanMove() && !_blockMovement;
    }

    public void DieFromFallingIntoDeepAndDarkAbbys()
    {
        StartCoroutine("DieSequence");
    }

    IEnumerator DieSequence()
    {
        _animator.SetBool("Dying", true);
        BlockMovement(true);
        yield return new WaitForSeconds(0.5f);
        _cameraController.RemoveFollowTarget(transform);
        yield return new WaitForSeconds(0.5f);

        _animator.SetBool("Dying", false);
        BlockMovement(false);
        _cameraController.AddFollowTarget(transform);
        _rigidbody.velocity = Vector3.zero;
        transform.position = _lastGroundPos;
    }
}
