using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GomezController : MonoBehaviour
{
    // movement variables
    [Header("Movement Variables")]
    public float maxSpeed = 2.0f;
    public float maxAirSpeed = 1.5f;
    public float groundAccel = 1.0f;
    public float airAccel = 0.5f;
    public float groundFriction = 0.1f;
    public float airFriction = 0.02f;
    public float jumpForce = 2.0f;
    public float jumpHoldMultiplier = 0.5f;
    public float regainControlBlockTime = 0.1f;
    public float maxGrabbedSpeed = 1f;
    public float maxGrabbedDistance = 2f;
    public float holdDistance = 1.5f;
    public LayerMask groundMask;
    public LayerMask passageMask;
    public LayerMask grabMask;

    // camera variables
    [Header("Camera Variables")]
    public float cameraRotInterp = 1.0f;
    public float blockTime = 0.7f;
    public float cameraFollowSpeed = 5f;
    public float cameraMaxHorizontalDistance = 2f;
    public float cameraVerticalOffset = 2f;
    public AnimationCurve cameraRotationMovement;
    public float cameraAspectRatio = 1.3333f;
    public float cameraSize = 4;

    [Header("Other")]
    public AudioClip shiftLeftSound;
    public AudioClip shiftRightSound;
    public AudioClip failedPickSound;
    public AudioClip successPickSound;
    public AudioClip dropSound;
    public AudioClip portalPassageSound;

    GameObject gameCamera;
    Camera gameCameraCamera;
    BoxCollider col;
    Rigidbody rigid;
    Animator anim;
    SpriteRenderer gomezSprite;
    SpriteAligner gomezAligner;
    AudioManager audioManager;

    Vector3 camFollowPoint;
    bool camFollowPlayer = true;
    float prevLookAng = 0;
    float lookAng = 0;
    float blockTimer = 0;

    GameObject grabbedEntity = null;

    // walking through portal
    bool tpRequestWalk = false;
    bool tpWalking = false;
    bool tpDoors = false;
    bool tpShiftedPersp = false;
    float tpWalkTime = 0;
    GameObject tpPortal = null;

    bool grounded = true;

    float wishDir = 0;
    int jumpState = 0;
    float prevVelY = 0;
    Vector3 lastGroundPos;

    bool blockMovement = false;
    bool cameraForcedThisFrame = false;

    public static GomezController main;

    void Start() {
        main = this;
        gameCamera = transform.Find("CameraHook").gameObject;
        gameCameraCamera = gameCamera.GetComponentInChildren<Camera>();
        col = GetComponent<BoxCollider>();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        gomezSprite = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        gomezAligner = transform.Find("Sprite").GetComponent<SpriteAligner>();
        audioManager = GetComponent<AudioManager>();

        var doors = LevelManager.GetInstance().entryDoors;
        if (doors) {
            lookAng = doors.transform.eulerAngles.y-180;
            prevLookAng = lookAng;

            blockTimer = 1.0f;
            transform.position = doors.transform.position + (doors.transform.forward * col.size.x + Vector3.up * col.size.y) * 0.5f;
            anim.Play("gomez_walkout");
        }

        lastGroundPos = transform.position;
    }

    void FixedUpdate() {
        // check grounded state
        grounded = Physics.CheckBox(
            transform.position - new Vector3(0, col.size.y * 0.5f, 0),
            new Vector3(col.size.x * 0.499f, 0.01f, col.size.z * 0.499f),
            Quaternion.identity,
            groundMask
        );

        // player movement
        if (CanMove()) {

            Vector3 vel = rigid.velocity;

            // bringing back previous y velocity
            // we reset x velocity, so that doesn't need to be saved
            if (prevVelY != 0) {
                vel.y = prevVelY;
                prevVelY = 0;
            }

            // jumping
            if (grounded && jumpState == 1) {
                vel.y = jumpForce;
                grounded = false;
                jumpState = 2;
            }
            if (vel.y < 0) {
                jumpState = 0;
            }

            // gravity
            if (!grounded) {
                vel.y += Physics.gravity.y * (jumpState == 2 ? jumpHoldMultiplier : 1);
            }

            // horizontal movement
            Vector3 moveDir = Quaternion.Euler(0, lookAng + 90, 0) * Vector3.forward;
            float curSpeed = Vector3.Dot(rigid.velocity, moveDir);

            curSpeed += (grounded ? groundAccel : airAccel) * wishDir;
            float maxCurSpeed = (grounded ? maxSpeed : maxAirSpeed);
            if (Mathf.Abs(curSpeed) > maxCurSpeed) {
                curSpeed = curSpeed / Mathf.Abs(curSpeed) * maxCurSpeed;
            }
            if (wishDir == 0) {
                curSpeed *= 1 - (grounded ? groundFriction : airFriction);
            }
            vel = moveDir * curSpeed + Vector3.up * vel.y;

            rigid.velocity = vel;


            Vector3 castDir = Quaternion.Euler(0, -90, 0) * moveDir;

            Handle2DLanding(castDir);
            Handle2DLanding(castDir * -1);

            
            for(int i = -1; i <= 1; i++) {
                // handle wall movement for forward faces.
                HandleWallMovement(castDir, i);
                // doing the same for back walls, except check for blocked flat jump (indoors movement)
                HandleWallMovement(-castDir, i, false);
            }


            if (tpRequestWalk) {
                if (grounded) {
                    RaycastHit hit;
                    bool canEnterPortal = Physics.Raycast(transform.position, Camera.main.transform.forward, out hit, 128.0f, passageMask);
                    if (canEnterPortal) {
                        var obj = hit.collider.gameObject;
                        var portal = obj.GetComponent<Portal>();
                        if (portal && portal.linkedPortal != null && Vector3.Dot(Camera.main.transform.forward, -hit.normal)>0.9) {
                            tpWalking = true;
                            tpPortal = obj;
                        } else {
                            if (LevelManager.GetInstance().TransitionToNextLevel(obj)) {
                                tpWalking = true;
                                tpDoors = true;
                                tpPortal = obj;
                                anim.Play("gomez_walkin");
                            }
                        }
                        
                    }
                }
                tpRequestWalk = false;
            }

            if (Physics.Raycast(transform.position - new Vector3(0, col.size.y * 0.49f, 0), Vector3.down, 0.02f, groundMask)) {
                lastGroundPos = transform.position;
            }

        } else if (tpWalking) {
            gomezAligner.enabled = false;
            col.enabled = false;
            rigid.velocity = Vector3.zero;
            tpWalkTime += Time.fixedDeltaTime;

            if (tpWalkTime < 0.5 || tpDoors) {
                float walkPortalOffset = (0.5f - tpWalkTime) * col.size.x;
                if (tpDoors) walkPortalOffset = 0.5f;

                Vector3 pos = transform.position;
                pos -= tpPortal.transform.forward * (Vector3.Dot(tpPortal.transform.forward, pos - tpPortal.transform.position) - walkPortalOffset);
                Vector3 desiredPos = tpPortal.transform.position + tpPortal.transform.forward * walkPortalOffset;
                if (tpDoors) {
                    desiredPos += Vector3.up * col.size.y * 0.5f;
                } else {
                    desiredPos -= Vector3.up * (1 - col.size.y * 0.5f);
                }
                transform.position = Vector3.MoveTowards(pos, desiredPos, maxSpeed*Time.fixedDeltaTime*0.25f);

                if (tpWalkTime < 0.2) {
                    tpShiftedPersp = false;
                }else if (!tpShiftedPersp && !tpDoors) {
                    GameObject linkedPortal = tpPortal.GetComponent<Portal>().linkedPortal;
                    float ang = Vector3.SignedAngle(tpPortal.transform.forward, linkedPortal.transform.forward, Vector3.up);

                    int rotations = (int)Mathf.Floor((Mathf.Abs(ang)+45)/90);
                    for(int i = 0; i < rotations; i++) {
                        RotateCamera(ang > 0 ? -1 : 1);
                    }

                    tpShiftedPersp = true;

                    audioManager.PlayClip(portalPassageSound);
                }

            }else if(tpWalkTime < 1.0) {
                float walkPortalOffset = (tpWalkTime - 0.49f) * col.size.x;
                GameObject linkedPortal = tpPortal.GetComponent<Portal>().linkedPortal;
                Vector3 desiredPos = linkedPortal.transform.position + linkedPortal.transform.forward * walkPortalOffset - Vector3.up * (1 - col.size.y * 0.5f);
                transform.position = desiredPos;
                gomezAligner.SetAngle(lookAng);
            } else {
                tpWalking = false;
                tpWalkTime = 0;
                col.enabled = true;
                gomezAligner.enabled = true;
                tpPortal = null;
            }
        } else {
            if(prevVelY == 0 && rigid.velocity.y != 0) {
                prevVelY = rigid.velocity.y;
            }
            rigid.velocity = Vector3.zero;
        }
    }

    void Update() {
        if (LevelManager.GetInstance().IsPaused()) {
            return;
        }

        if (!blockMovement) {
            wishDir = 0;
            if (Input.GetKey(KeyCode.A)) {
                wishDir -= 1;
            }
            if (Input.GetKey(KeyCode.D)) {
                wishDir += 1;
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                if (jumpState == 0) jumpState = 1;
            } else if (!Input.GetKey(KeyCode.Space)) {
                jumpState = 0;
            }

            if (Input.GetKeyDown(KeyCode.W) && CanControl()) {
                tpRequestWalk = true;
            }
        }


        // camera perspective rotation
        if (!tpWalking && !(blockTimer>0 && lookAng==prevLookAng) && !blockMovement) {
            if (Input.GetKeyDown(KeyCode.Q)) {
                RotateCamera(-1);
                SpeedrunValues.shiftCount++;
            }
            if (Input.GetKeyDown(KeyCode.E)) {
                RotateCamera(1);
                SpeedrunValues.shiftCount++;
            }
        }

        if(CanMove() || tpWalking || lookAng == prevLookAng) {
            Time.timeScale = 1;
        } else {
            Time.timeScale = 0;
        }

        blockTimer = Mathf.Max(0, blockTimer - Mathf.Min(Time.unscaledDeltaTime,0.1f));

        UpdateGrabbedObject();

        UpdateCameraPosition();

        UpdateAnimator();
    }


    void RotateCamera(int dir) {
        prevLookAng = gameCamera.transform.eulerAngles.y;
        float ang = dir > 0 ? -90 : (dir < 0 ? 90 : 0);
        lookAng = (lookAng + ang) % 360.0f;
        blockTimer = blockTime;

        if (dir < 0) audioManager.PlayClip(shiftLeftSound);
        if (dir > 0) audioManager.PlayClip(shiftRightSound);
    }

    // handle 2D movement "through" walls
    bool HandleWallMovement(Vector3 castDir, int vOffset, bool allowFlatJump=true) {
        // doing two raycasts: one to detect if we're not already obscured by a wall
        // if we do, doing another one to check if we will be obscured by a wall
        float hitboxWidth = col.size.x;
        const float castDist = 64f;
        Vector3 startPos = transform.position - castDir * castDist + Vector3.up * vOffset * col.size.y * 0.49f;
        if(vOffset==0) startPos += Vector3.down * 0.005f;
        RaycastHit hit;
        for (int i = 0; i < 2; i++) {
            float castLength = castDist + hitboxWidth * 0.4999f;
            if (i == 1) {
                Vector3 moveVel = rigid.velocity;
                if (moveVel.y < 0) moveVel.y = 0;
                Vector3 horVer = new Vector3(moveVel.x, 0, moveVel.z);
                if (horVer.magnitude != 0) startPos += horVer.normalized * hitboxWidth * 0.5f;
                startPos += moveVel * Time.fixedDeltaTime;
                castLength = castDist + hitboxWidth * 0.5001f;
            }
            RaycastHit[] hits = Physics.RaycastAll(startPos, castDir, castLength, groundMask, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0) {
                hit = hits[0];
                foreach (RaycastHit h in hits) {
                    if(h.distance > hit.distance) {
                        hit = h;
                    }
                }
                float hitForce = -Vector3.Dot(hit.normal, castDir);
                if (hitForce > 0.01) {
                    if (i == 0) {
                        Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red);
                        return false;
                    } else {
                        float spaceBetweenWall = 1.1f;
                        float flatJumpForce = (castDist - hit.distance + hitboxWidth * 0.5f * (spaceBetweenWall + (1f-hitForce)*2.0f));
                        // additional raycast to check if illegal jump occurs
                        if (!allowFlatJump) {
                            RaycastHit flatHit;
                            Vector3 flatStartPos = startPos + castDir * (castDist - hitboxWidth * 0.5f);
                            bool blockedFlatJump = Physics.Raycast(flatStartPos, -castDir, out flatHit, flatJumpForce, groundMask, QueryTriggerInteraction.Ignore);
                            if (blockedFlatJump) {
                                Debug.DrawLine(hit.point, hit.point + hit.normal, new Color(1.0f,0.5f,0.0f));
                                Debug.DrawLine(flatHit.point, flatHit.point + flatHit.normal, new Color(1.0f, 0.5f, 0.5f));
                                return false;
                            }
                        }
                        //block jump if there is no space
                        RaycastHit spaceHit;
                        bool blockedSpace = Physics.Raycast(hit.point, hit.normal, out spaceHit, hitboxWidth,groundMask, QueryTriggerInteraction.Ignore);
                        if (blockedSpace) {
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

    void Handle2DLanding(Vector3 castDir) {
        if (grounded || rigid.velocity.y > 0) return;

        //check if we're not about to land on a ground
        bool aboutToLand = Physics.CheckBox(
            transform.position - new Vector3(0, col.size.y * 0.5f, 0),
            new Vector3(col.size.x * 0.499f, Mathf.Abs(rigid.velocity.y) * Time.fixedDeltaTime * 2.1f, col.size.z * 0.499f),
            Quaternion.identity,
            groundMask
        );

        if (aboutToLand) return;


        // now we can actually do the landing logic
        const float castDist = 64f;

        Vector3 startPos = transform.position + Vector3.down * col.size.y * 0.5f;
        Vector3 startPosAfterMove = startPos + rigid.velocity * Time.fixedDeltaTime;

        Vector3 sideVec = Vector3.Cross(Vector3.up, castDir.normalized);
        RaycastHit groundHit, farHit;
        for(int i = -1; i <= 1; i += 2) {
            Vector3 o = sideVec * i * col.size.x * 0.49f;
            bool foundGround = Physics.Raycast(startPosAfterMove + o, castDir, out groundHit, castDist, groundMask, QueryTriggerInteraction.Ignore);
            if (foundGround) {
                bool foundFar = Physics.BoxCast(
                    startPos + Vector3.up*0.1f, 
                    new Vector3(col.size.x,0.1f,0.1f), 
                    castDir, out farHit, Camera.main.transform.rotation, 
                    castDist, groundMask, QueryTriggerInteraction.Ignore
                );
                if(!foundFar || farHit.distance - col.size.x > groundHit.distance) {
                    transform.position = transform.position + castDir.normalized * (groundHit.distance + col.size.x * 0.51f);
                    Debug.DrawLine(groundHit.point, groundHit.point + groundHit.normal, Color.cyan);
                    Debug.DrawLine(groundHit.point, startPos + o, Color.cyan);
                    return;
                }
            }
        }
    }

    void UpdateGrabbedObject() {
        if (grabbedEntity) {
            Vector3 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 targetPos = transform.position;

            Vector3 lookDir = mousePoint - targetPos;
            lookDir -= Camera.main.transform.forward * Vector3.Dot(Camera.main.transform.forward, lookDir);
            lookDir = lookDir.normalized * holdDistance;
            targetPos += lookDir;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, lookDir.normalized, out hit, lookDir.magnitude + 1.0f, groundMask)) {
                targetPos = transform.position + Vector3.up * col.size.y;
            }
            RaycastHit mouseHit;
            Vector3 lookPoint;
            if (Physics.Raycast(mousePoint, Camera.main.transform.forward, out mouseHit, 128.0f, groundMask)) {
                lookPoint = mouseHit.point;
            } else {
                lookPoint = transform.position + (mousePoint - Camera.main.transform.position);
            }

            lookPoint -= Vector3.up * Vector3.Dot(Vector3.up, lookPoint - grabbedEntity.transform.position);

            grabbedEntity.transform.position = Vector3.MoveTowards(grabbedEntity.transform.position, targetPos, maxGrabbedSpeed * Time.deltaTime);
            Quaternion targetRot = Quaternion.LookRotation(lookPoint - grabbedEntity.transform.position, Vector3.up);
            grabbedEntity.transform.rotation = Quaternion.RotateTowards(grabbedEntity.transform.rotation, targetRot, maxGrabbedSpeed * 20.0f * Time.deltaTime);

            Vector3 eulerRot = grabbedEntity.transform.localEulerAngles;
            grabbedEntity.transform.localEulerAngles = new Vector3(0, eulerRot.y, 0);

            if (Input.GetKeyDown(KeyCode.LeftControl)) {
                grabbedEntity.transform.position = targetPos;
                grabbedEntity.layer = LayerMask.NameToLayer("Grabbable");
                Physics.IgnoreCollision(grabbedEntity.GetComponent<Collider>(), col, false);
                grabbedEntity = null;
                audioManager.PlayClip(dropSound);
            }
        } else if(CanControl()) {
            if (Input.GetKeyDown(KeyCode.LeftControl)) {
                Vector3 clickPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(clickPoint, Camera.main.transform.forward, out hit, 128.0f, groundMask)) {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grabbable")) {
                        // attempting to grab an object. check if 2d distance is shorter than max grab distance
                        Vector3 dist = hit.collider.gameObject.transform.position - transform.position;
                        dist -= Camera.main.transform.forward * Vector3.Dot(Camera.main.transform.forward, dist);
                        if (dist.magnitude < maxGrabbedDistance) {
                            grabbedEntity = hit.collider.gameObject;
                            hit.collider.gameObject.layer = LayerMask.NameToLayer("Held");
                            Physics.IgnoreCollision(grabbedEntity.GetComponent<Collider>(), col, true);
                            anim.Play("gomez_shoot", 0, 0.0f);
                            FlipSprite(Vector3.Dot(Camera.main.transform.right, hit.collider.transform.position - transform.position) < 0);
                            audioManager.PlayClip(successPickSound);
                        }
                    }
                }
                if (!grabbedEntity) {
                    audioManager.PlayClip(failedPickSound);
                }
            }
        }
    }

    public Vector3 GetActualCameraFollowPoint() {
        Vector3 followPoint = camFollowPoint + Vector3.up * cameraVerticalOffset;

        float hVel = Vector3.Dot(gameCamera.transform.right, followPoint - gameCamera.transform.position);
        float hDVel = 0;
        if (hVel != 0) hDVel = (hVel / Mathf.Abs(hVel)) * Mathf.Max(0, Mathf.Abs(hVel) - cameraMaxHorizontalDistance);

        followPoint += gameCamera.transform.right * (hDVel - hVel);

        return followPoint;
    }

    // updating camera size to not go above given aspect ratio (avoids seeing unwanted surfaces on high width and low height)
    public float GetActualCameraSize() {
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float size = cameraSize;
        if (screenRatio > cameraAspectRatio) {
            size = cameraSize * (cameraAspectRatio / screenRatio);
        }
        return size;
    }

    void UpdateCameraPosition() {

        // calculating follow point (it has certain horizontal distance it can keep without following)

        if (camFollowPlayer) {
            camFollowPoint = transform.position;
        }

        if (cameraForcedThisFrame) {
            cameraForcedThisFrame = false;
            return;
        }

        Vector3 followPoint = GetActualCameraFollowPoint();

        // updating the position
        if (gameCamera.transform.parent) {
            gameCamera.transform.parent = null;
            gameCamera.transform.position = followPoint;
        } else {
            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, followPoint, cameraFollowSpeed * Time.unscaledDeltaTime);
        }

        // updating the rotation.
        float ang = lookAng;
        if (blockTimer > 0) {
            float t = cameraRotationMovement.Evaluate(1 - (blockTimer / blockTime));
            ang = Mathf.LerpAngle(prevLookAng, lookAng, t);
        }
        gameCamera.transform.rotation = Quaternion.Euler(0, ang, 0);

        gameCameraCamera.orthographicSize = GetActualCameraSize();
    }


    void UpdateAnimator() {
        if (CanMove() || tpWalking || lookAng == prevLookAng) {
            anim.SetBool("Grounded", grounded);

            Vector3 vel = rigid.velocity;

            float jumpState = Mathf.Clamp(-vel.y / (jumpForce * 2) + 0.5f, 0, 1);
            anim.SetFloat("FlyState", jumpState);


            vel.y = 0;
            float horizontalVel = vel.magnitude;

            anim.SetBool("Walking", horizontalVel > 0.1f);
            anim.SetBool("Running", horizontalVel > maxSpeed * 0.6f);

            anim.SetInteger("Timer", (anim.GetInteger("Timer") + 1) % 1000);
            if (Random.Range(0, 1000) < 2) anim.SetTrigger("Blink");

            float moveDir = Vector3.Dot(vel, gameCamera.transform.right);

            int flipDir = 0;
            int edgeFlipDir = 0;

            if (wishDir > 0) {
                flipDir = 1;
            } else if (wishDir < 0) {
                flipDir = -1;
            }

            bool standingOnEdge = false;
            if (grounded && rigid.velocity.magnitude < 0.01f) {
                Vector3 startPos = transform.position + Vector3.down * col.size.y * 0.49f;
                Vector3 edgeOffset = Camera.main.transform.right * col.size.x * 0.5f;
                if(!Physics.Raycast(startPos, Vector3.down, 0.1f, groundMask)) {
                    for (int i = -1; i <= 1; i += 2) {
                        bool hasEdge = Physics.Raycast(startPos + edgeOffset * i, Vector3.down, 0.1f, groundMask);
                        if (!hasEdge) {
                            standingOnEdge = !standingOnEdge; // prevents activating the animation when standing "midair"
                            edgeFlipDir = i;
                        }
                    }
                }
            }

            if (standingOnEdge) flipDir = edgeFlipDir;
            if (flipDir!=0) FlipSprite(flipDir < 0 ? true : false);

            anim.SetBool("OnEdge", standingOnEdge);

            // special cases

            if (moveDir * wishDir < -0.1 && grounded) {
                anim.Play("gomez_drift");
            }

            if( tpWalking && !tpDoors) {
                if(tpWalkTime < 0.1) {
                    anim.Play("gomez_walkin");
                }else if(tpWalkTime >= 0.5 && tpWalkTime <= 0.6) {
                    anim.Play("gomez_walkout",0,0.3f);
                }
            }

            anim.speed = 1;
        } else {
            anim.speed = 0;
        }
    }

    public void FlipSprite(bool flipped) {
        gomezSprite.transform.localScale = new Vector3(flipped ? -1 : 1, 1, 1);
    }

    public void BlockMovement(bool block) {
        blockMovement = block;
    }

    public void SetCameraPositionThisFrame(Vector3 pos, float size=-1, float angle=-999) {
        gameCamera.transform.position = pos;
        if(size>0) gameCameraCamera.orthographicSize = size;
        if(angle!=-999) gameCamera.transform.rotation = Quaternion.Euler(0, angle, 0);

        cameraForcedThisFrame = true;
    }

    public static bool CanMove() {
        if (main) {
            return main.blockTimer < main.regainControlBlockTime && !main.tpWalking;
        } else return false; // just in case there is no player
    }

    public static bool CanControl() {
        if (main) {
            return CanMove() && !main.blockMovement;
        } else return false; // just in case there is no player
    }

    public void DieFromFallingIntoDeepAndDarkAbbys() {
        StartCoroutine("DieSequence");
    }

    IEnumerator DieSequence() {
        anim.SetBool("Dying", true);
        BlockMovement(true);
        yield return new WaitForSeconds(0.5f);
        camFollowPlayer = false;
        yield return new WaitForSeconds(0.5f);

        anim.SetBool("Dying", false);
        BlockMovement(false);
        camFollowPlayer = true;
        rigid.velocity = Vector3.zero;
        transform.position = lastGroundPos;
    }
}
