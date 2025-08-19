using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class OneWayTeleporter : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private Transform destination;
    [SerializeField] private Vector3 exitLocalOffset = new Vector3(0f, 0f, 1f);
    [SerializeField] private bool alignToDestinationRotation = true;

    [Header("Filtering")]
    [SerializeField]
    private string requiredTag = "Player";

    [Header("Velocity")]
    [SerializeField] private bool preserveVelocity = true;
    [SerializeField] private bool mapVelocityFromSourceToDestination = true;
    [SerializeField] private float extraExitSpeed;

    [Header("Safety")]
    [SerializeField] private float reTriggerCooldown = 0.2f;
    [SerializeField] private bool temporarilyIgnoreTeleporterColliders = true;

    private readonly Dictionary<int, float> _lastTeleportTime = new();

    private void Reset()
    {
        Collider jumpPadCollider = GetComponent<Collider>();
        if (jumpPadCollider) jumpPadCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => TryTeleport(other);

    private void TryTeleport(Collider other)
    {
        if (!destination) return;

        GameObject root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        if (!string.IsNullOrEmpty(requiredTag) && !root.CompareTag(requiredTag)) return;

        int id = root.GetInstanceID();
        float now = Time.time;
        if (_lastTeleportTime.TryGetValue(id, out float last) && (now - last) < reTriggerCooldown) return;

        Rigidbody jumpPadRigidbody = root.GetComponent<Rigidbody>();
        Vector3 targetPos = destination.TransformPoint(exitLocalOffset);
        Quaternion targetRot = alignToDestinationRotation ? destination.rotation : root.transform.rotation;

        if (jumpPadRigidbody)
        {
            Vector3 v = jumpPadRigidbody.velocity;

            if (preserveVelocity)
            {
                if (mapVelocityFromSourceToDestination)
                {
                    Vector3 localVector = transform.InverseTransformDirection(v);
                    v = destination.TransformDirection(localVector);
                }
            }
            
            else
            {
                v = Vector3.zero;
            }

            if (extraExitSpeed != 0f)
                v += destination.forward * extraExitSpeed;

            jumpPadRigidbody.position = targetPos;
            jumpPadRigidbody.rotation = targetRot;
            jumpPadRigidbody.velocity = v;

            if (temporarilyIgnoreTeleporterColliders)
                StartCoroutine(TempIgnore(root));
        }
        else
        {
            root.transform.SetPositionAndRotation(targetPos, targetRot);
        }

        _lastTeleportTime[id] = now;
    }

    private IEnumerator TempIgnore(GameObject root)
    {
        Collider[] myCols = GetComponents<Collider>();
        Collider[] otherCols = root.GetComponentsInChildren<Collider>();
        
        foreach (Collider a in myCols)
            foreach (Collider b in otherCols)
                if (a && b) Physics.IgnoreCollision(a, b, true);

        yield return new WaitForFixedUpdate();

        foreach (Collider a in myCols)
            foreach (Collider b in otherCols)
                if (a && b) Physics.IgnoreCollision(a, b, false);
    }

    private void OnDrawGizmos()
    {
        if (!destination) return;
        Gizmos.color = Color.magenta;
        Vector3 to = destination.TransformPoint(exitLocalOffset);
        Gizmos.DrawLine(transform.position, to);
        Gizmos.DrawWireSphere(to, 0.15f);
        Gizmos.DrawRay(to, destination.forward * 0.4f);
    }
}