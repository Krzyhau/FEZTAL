using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class FezPhysicsObject : MonoBehaviour
{
    [SerializeField] private BoxCollider _collider;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Physics Object Properties")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _2DCastDistance = 64.0f;

    private void FixedUpdate()
    {
        Handle2DMovement();
    }

    private void Handle2DMovement()
    {
        Vector3 castDir = Quaternion.Euler(0.0f, LevelManager.Camera.PhysicsAngle, 0.0f) * Vector3.forward;

        // handle 2D landing before all else - we don't want to phase through the floor when going down
        Handle2DLanding(castDir);
        Handle2DLanding(castDir * -1);

        for (int i = -1; i <= 1; i++)
        {
            // handle wall movement for forward faces.
            Handle2DWallMovement(castDir, i);
            // doing the same for back walls, except check for blocked flat jump (indoors movement)
            Handle2DWallMovement(-castDir, i, false);
        }
    }

    // handle 2D movement "through" walls
    private bool Handle2DWallMovement(Vector3 castDir, int vOffset, bool allowFlatJump = true)
    {
        // doing two raycasts: one to detect if we're not already obscured by a wall
        // if we do, doing another one to check if we will be obscured by a wall
        float hitboxWidth = _collider.size.x;
        Vector3 startPos = transform.position - castDir * _2DCastDistance + Vector3.up * vOffset * _collider.size.y * 0.49f;
        if (vOffset == 0) startPos += Vector3.down * 0.005f;
        RaycastHit hit;
        for (int i = 0; i < 2; i++)
        {
            float castLength = _2DCastDistance + hitboxWidth * 0.4999f;
            if (i == 1)
            {
                Vector3 moveVel = _rigidbody.linearVelocity;
                moveVel.y = Mathf.Max(moveVel.y, 0);
                Vector3 horVer = new Vector3(moveVel.x, 0, moveVel.z);
                if (horVer.magnitude != 0) startPos += horVer.normalized * hitboxWidth * 0.5f;
                startPos += moveVel * Time.fixedDeltaTime;
                castLength = _2DCastDistance + hitboxWidth * 0.5001f;
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
                    } else
                    {
                        float spaceBetweenWall = 1.1f;
                        float flatJumpForce = (_2DCastDistance - hit.distance + hitboxWidth * 0.5f * (spaceBetweenWall + (1f - hitForce) * 2.0f));
                        // additional raycast to check if illegal jump occurs
                        if (!allowFlatJump)
                        {
                            RaycastHit flatHit;
                            Vector3 flatStartPos = startPos + castDir * (_2DCastDistance - hitboxWidth * 0.5f);
                            bool blockedFlatJump = Physics.Raycast(flatStartPos, -castDir, out flatHit, flatJumpForce, _groundMask, QueryTriggerInteraction.Ignore);
                            if (blockedFlatJump)
                            {
                                Debug.DrawLine(hit.point, hit.point + hit.normal, new Color(1.0f, 0.5f, 0.0f));
                                Debug.DrawLine(flatHit.point, flatHit.point + flatHit.normal, new Color(1.0f, 0.5f, 0.5f));
                                return false;
                            }
                        }
                        //block jump if there is no space
                        bool blockedSpace = Physics.Raycast(hit.point, hit.normal, out var spaceHit, hitboxWidth, _groundMask, QueryTriggerInteraction.Ignore);
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

    private void Handle2DLanding(Vector3 castDir)
    {
        if (_rigidbody.linearVelocity.y >= 0) return;

        //check if we're not about to land on a ground
        bool aboutToLand = Physics.CheckBox(
            transform.position - new Vector3(0, _collider.size.y * 0.5f, 0),
            new Vector3(_collider.size.x * 0.499f, Mathf.Abs(_rigidbody.linearVelocity.y) * Time.fixedDeltaTime * 2.1f, _collider.size.z * 0.499f),
            Quaternion.identity,
            _groundMask
        );

        if (aboutToLand) return;

        // now we can actually do the landing logic
        Vector3 startPos = transform.position + Vector3.down * _collider.size.y * 0.5f;
        Vector3 startPosAfterMove = startPos + _rigidbody.linearVelocity * Time.fixedDeltaTime;

        Vector3 sideVec = Vector3.Cross(Vector3.up, castDir.normalized);
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 o = sideVec * i * _collider.size.x * 0.49f;
            bool foundGround = Physics.Raycast(startPosAfterMove + o, castDir, out var groundHit, _2DCastDistance, _groundMask, QueryTriggerInteraction.Ignore);
            if (foundGround)
            {
                bool foundFar = Physics.BoxCast(
                    startPos + Vector3.up * 0.1f,
                    new Vector3(_collider.size.x, 0.1f, 0.1f),
                    castDir, out var farHit, Camera.main.transform.rotation,
                    _2DCastDistance, _groundMask, QueryTriggerInteraction.Ignore
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
}
