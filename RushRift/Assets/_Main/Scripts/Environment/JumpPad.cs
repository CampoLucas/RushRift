using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    public enum DirectionSource
    {
        WorldUp,
        PadUp,
        CustomVector,
        TransformForward
    }

    [SerializeField] float launchSpeed = 12f;
    [SerializeField] DirectionSource directionSource = DirectionSource.PadUp;
    [SerializeField] Vector3 customDirection = Vector3.up;
    [SerializeField] Transform directionTransform;
    [SerializeField] bool resetTangentialVelocity = false;
    [SerializeField] bool triggerOnlyOnEnter = true;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnEnter) TryLaunch(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!triggerOnlyOnEnter) TryLaunch(other);
    }

    void TryLaunch(Collider other)
    {
        var rb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();
        if (!rb) return;
        if (!rb.gameObject.CompareTag("Player")) return;

        var dir = GetDirection();
        var v = rb.velocity;

        float along = Vector3.Dot(v, dir);
        var tangential = v - dir * along;
        if (resetTangentialVelocity) tangential = Vector3.zero;

        rb.velocity = tangential + dir * Mathf.Max(launchSpeed, 0f);
    }

    Vector3 GetDirection()
    {
        Vector3 dir;
        switch (directionSource)
        {
            case DirectionSource.WorldUp:
                dir = Vector3.up;
                break;
            case DirectionSource.PadUp:
                dir = transform.up;
                break;
            case DirectionSource.CustomVector:
                dir = customDirection;
                break;
            case DirectionSource.TransformForward:
                dir = directionTransform ? directionTransform.forward : Vector3.forward;
                break;
            default:
                dir = transform.up;
                break;
        }

        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;
        return dir.normalized;
    }

    void OnDrawGizmosSelected()
    {
        var dir = GetDirection();
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, dir * Mathf.Max(launchSpeed * 0.25f, 0.25f));
    }
}
