using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class FezPhysicsObject : MonoBehaviour
{
    private const float CONTACT_OFFSET = 0.01f;
    private static RaycastHit[] _raycastHitBuffer = new RaycastHit[256];
    
    [SerializeField] private BoxCollider _collider;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Physics Object Properties")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _2DCastDistance = 64.0f;

    private void FixedUpdate()
    {
        DEBUG_Update();
        Handle2DMovement();
    }

    private void Handle2DMovement()
    {
        if (Mathf.Approximately(_rigidbody.linearVelocity.sqrMagnitude, 0f))
        {
            return;
        }
        
        Vector3 projectionNormal = GetMovementProjectionNormal();

        Vector3 movementAdjustmentProjectionNormal = projectionNormal * (IsCovered(projectionNormal) ? -1.0f : 1.0f);
        AdjustForProjectedMovement(movementAdjustmentProjectionNormal);
    }
    
    private Vector3 GetMovementProjectionNormal()
    {
        return Quaternion.Euler(0.0f, LevelManager.Camera.PhysicsAngle, 0.0f) * Vector3.forward;
    }

    private bool IsCovered(Vector3 projectionAxis)
    {
        var fromBackOrigin = _rigidbody.position - projectionAxis * _2DCastDistance;
        float spaceFromBack = MeasureObjectBoxCast(fromBackOrigin, projectionAxis, _2DCastDistance);
        DEBUG_DrawBox(Color.blue, fromBackOrigin + projectionAxis * spaceFromBack);
        return spaceFromBack < _2DCastDistance - _collider.size.z * 0.5f;
    }
    
    private void AdjustForProjectedMovement(Vector3 projectionAxis)
    {
        var backwardsAxis = projectionAxis * -1.0f;
        var availableSpace = MeasureObjectBoxCast(_rigidbody.position, backwardsAxis, _2DCastDistance);
        
        var movementStep = GetProjectedMovementStep(projectionAxis);
        var movementStepDistance = movementStep.magnitude + CONTACT_OFFSET;
        var movementStepDirection = movementStep.normalized;
        
        var potentialWarpPlacesFindOrigin = _rigidbody.position + backwardsAxis * availableSpace + movementStep;
        
        var bestPlaceAdjustmentDistance = 0f;
        var bestPlaceDistanceToCollision = MeasureObjectBoxCast(_rigidbody.position, movementStepDirection, movementStepDistance);
        
        foreach (var potentialWarpPlaceHit in IterateObjectBoxCasts(potentialWarpPlacesFindOrigin, projectionAxis, availableSpace))
        {
            var potentialAdjustmentDistance = availableSpace - potentialWarpPlaceHit.distance + CONTACT_OFFSET;
            var potentialWarpPlaceCheckOrigin = _rigidbody.position + backwardsAxis * potentialAdjustmentDistance;
            
            var distanceToCollision = MeasureObjectBoxCast(potentialWarpPlaceCheckOrigin, movementStepDirection, movementStepDistance);

            var collidedFromMovement = distanceToCollision < movementStepDistance;
            var isBetterWarpPlace =
                (!collidedFromMovement && potentialAdjustmentDistance > bestPlaceAdjustmentDistance) ||
                (collidedFromMovement && distanceToCollision - CONTACT_OFFSET > bestPlaceDistanceToCollision);
            
            DEBUG_DrawBox(Color.darkGreen, potentialWarpPlaceCheckOrigin);
            DEBUG_DrawBox(Color.green, potentialWarpPlaceCheckOrigin + movementStepDirection * distanceToCollision);
            DEBUG_DrawText(potentialWarpPlaceCheckOrigin, $"Collision factor: {distanceToCollision / movementStepDistance}");
            
            if (isBetterWarpPlace)
            {
                bestPlaceAdjustmentDistance = potentialAdjustmentDistance;
                bestPlaceDistanceToCollision = distanceToCollision;
            }
        }
        
        _rigidbody.position += backwardsAxis * bestPlaceAdjustmentDistance;
    }

    private Vector3 GetProjectedMovementStep(Vector3 projectionAxis)
    {
        var projectedMovement = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, projectionAxis);
        
        var projectedMovementDirection = projectedMovement.normalized;
        var projectedMovementDistance = projectedMovement.magnitude * Time.fixedDeltaTime + CONTACT_OFFSET;
        
        return projectedMovementDirection * projectedMovementDistance;
    }
    
    private float MeasureObjectBoxCast(Vector3 origin, Vector3 direction, float distance)
    {
        float minDistance = distance;
        foreach (var hit in IterateObjectBoxCasts(origin, direction, distance))
        {
            minDistance = Mathf.Min(minDistance, hit.distance - CONTACT_OFFSET);
        }
        return minDistance;
    }
    
    private IEnumerable<RaycastHit> IterateObjectBoxCasts(Vector3 origin, Vector3 direction, float distance)
    {
        var adjustedColliderSize = _collider.size - Vector3.one * CONTACT_OFFSET;
        
        var hitCount = Physics.BoxCastNonAlloc(origin, adjustedColliderSize * 0.5f, direction, 
            _raycastHitBuffer, transform.rotation, distance, _groundMask);
        
        for (var i = 0; i < hitCount; i++)
        {
            var hit = _raycastHitBuffer[i];
            if (Mathf.Approximately(hit.distance, 0f) || hit.collider == _collider)
            {
                continue;
            }
            yield return _raycastHitBuffer[i];
        }
    }

    
#if UNITY_EDITOR
    private readonly List<(Color Color, Vector3 Position, float UntilTimestamp)> _debugBoxBuffer = new();
    private readonly List<(Vector3 Position, string Text, float UntilTimestamp)> _debugTextBuffer = new();
    
    private void DEBUG_DrawBox(Color color, Vector3 position, float time = 0.0f)
    {
        _debugBoxBuffer.Add((color, position, Time.time + time));
    }

    private void DEBUG_DrawText(Vector3 position, string text, float time = 0.0f)
    {
        _debugTextBuffer.Add((position, text, Time.time + time));
    }
    
    private void OnDrawGizmos()
    {
        foreach (var debugBox in _debugBoxBuffer)
        {
            Gizmos.color = debugBox.Color;
            Gizmos.DrawWireCube(debugBox.Position, _collider.size);
        }
        
        foreach (var debugText in _debugTextBuffer)
        {
            UnityEditor.Handles.Label(debugText.Position, debugText.Text, DEBUG_GetTextStyle());
        }
    }

    private GUIStyle DEBUG_GetTextStyle()
    {
        return new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };
    }

    private void DEBUG_Update()
    {
        _debugBoxBuffer.RemoveAll(debugBox => Time.time >= debugBox.UntilTimestamp);
        _debugTextBuffer.RemoveAll(debugText => Time.time >= debugText.UntilTimestamp);
    }
#else
    private void DEBUG_DrawText(Vector3 p, string t, float t = 0.0f) {}
    private void DEBUG_DrawBox(int i, Color c, Vector3 p, float t = 0.0f) { }
    private void DEBUG_Update() { }
#endif
}
