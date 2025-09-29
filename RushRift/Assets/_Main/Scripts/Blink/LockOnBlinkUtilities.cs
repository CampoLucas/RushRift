using _Main.Scripts.Feedbacks;
using Game;
using Game.Entities;
using UnityEngine;

public static class LockOnBlinkUtilities
{
    public static Transform AcquireTargetRaw(Camera cam, float radius, float maxDistance, LayerMask layers, string requiredTag, bool requireLos, RaycastHit[] hitsBuffer)
    {
        if (!cam) return null;
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.SphereCastNonAlloc(ray, radius, hitsBuffer, maxDistance, layers, QueryTriggerInteraction.Ignore);

        Transform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            ref var h = ref hitsBuffer[i];
            var tr = h.collider ? h.collider.transform : null;
            if (!tr) continue;

            if (!string.IsNullOrEmpty(requiredTag) && !tr.CompareTag(requiredTag)) continue;

            if (requireLos)
            {
                Vector3 dir = (h.point - cam.transform.position).normalized;
                if (Physics.Raycast(cam.transform.position, dir, out var block, h.distance - 0.01f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (block.collider.transform != tr && !IsChildOf(block.collider.transform, tr)) continue;
                }
            }

            if (h.distance < bestDist) { bestDist = h.distance; best = tr; }
        }

        return best;
    }

    public static Transform CanonicalizeTarget(Transform t)
    {
        if (!t) return null;
        var controller = t.GetComponentInParent<EntityController>();
        if (controller && controller.Origin) return controller.Origin.transform;
        return t;
    }

    public static Transform ApplyStickiness(Transform chargingTarget, Transform candidate, Camera cam, float now, ref float lastSeenTime, ref Vector3 lastDirFromCam, ref float lastDistFromCam, float angleToleranceDeg, float distanceToleranceMeters, float retainGraceSeconds)
    {
        if (!chargingTarget && !candidate) return null;

        if (!chargingTarget && candidate)
        {
            lastSeenTime = now;
            CacheTargetMetrics(cam, candidate, ref lastDirFromCam, ref lastDistFromCam);
            return candidate;
        }

        if (candidate)
        {
            var canonicalCandidate = CanonicalizeTarget(candidate);
            if (canonicalCandidate == chargingTarget)
            {
                lastSeenTime = now;
                CacheTargetMetrics(cam, canonicalCandidate, ref lastDirFromCam, ref lastDistFromCam);
                return canonicalCandidate;
            }

            if (cam)
            {
                Vector3 camPos = cam.transform.position;
                Vector3 v = canonicalCandidate.position - camPos;
                float newDist = v.magnitude;
                Vector3 newDir = newDist > 0f ? v / newDist : Vector3.forward;

                float angleDelta = Vector3.Angle(lastDirFromCam, newDir);
                float distDelta = Mathf.Abs(newDist - lastDistFromCam);

                if (angleDelta <= Mathf.Max(0f, angleToleranceDeg) && distDelta <= Mathf.Max(0f, distanceToleranceMeters))
                {
                    lastSeenTime = now;
                    return chargingTarget;
                }

                lastSeenTime = now;
                CacheTargetMetrics(cam, canonicalCandidate, ref lastDirFromCam, ref lastDistFromCam);
                return canonicalCandidate;
            }

            lastSeenTime = now;
            CacheTargetMetrics(cam, canonicalCandidate, ref lastDirFromCam, ref lastDistFromCam);
            return canonicalCandidate;
        }
        else
        {
            if (chargingTarget && (now - lastSeenTime) <= Mathf.Max(0f, retainGraceSeconds))
                return chargingTarget;

            return null;
        }
    }

    public static float ComputeDynamicLockRadius(bool chargingActive, float lockOnTimeSeconds, float lockTimer, float baseRadius, float chargingRadius, AnimationCurve ramp)
    {
        if (!chargingActive) return Mathf.Max(0f, baseRadius);
        float progress = Mathf.Clamp01(lockOnTimeSeconds > 0f ? lockTimer / lockOnTimeSeconds : 1f);
        float k = Mathf.Clamp01(ramp != null ? ramp.Evaluate(progress) : progress);
        float baseR = Mathf.Max(0f, baseRadius);
        float maxR = Mathf.Max(baseR, chargingRadius);
        return Mathf.Lerp(baseR, maxR, k);
    }

    public static void StartLockVisualFx(bool enable, float chromaTarget, float chromaIn, float vignTarget, float vignIn, bool unscaled)
    {
        if (!enable) return;
        if (ChromaticAberrationPlayer.GlobalInstance) ChromaticAberrationPlayer.GlobalInstance.ChromaticTween(0f, Mathf.Max(0f, chromaTarget), Mathf.Max(0f, chromaIn), unscaled);
        VignettePlayer.VignetteTweenGlobal(0f, Mathf.Max(0f, vignTarget), Mathf.Max(0f, vignIn), unscaled);
    }

    public static void StopLockVisualFx(bool enable, float chromaTarget, float chromaOut, float vignTarget, float vignOut, bool unscaled)
    {
        if (!enable) return;
        if (ChromaticAberrationPlayer.GlobalInstance) ChromaticAberrationPlayer.GlobalInstance.ChromaticTween(Mathf.Max(0f, chromaTarget), 0f, Mathf.Max(0f, chromaOut), unscaled);
        VignettePlayer.VignetteTweenGlobal(Mathf.Max(0f, vignTarget), 0f, Mathf.Max(0f, vignOut), unscaled);
    }

    public static void PlayLockSfx(bool enabled, string name)
    {
        if (!enabled || string.IsNullOrEmpty(name)) return;
        AudioManager.Play(name);
    }

    public static void StopLockSfx(bool enabled, string name)
    {
        if (!enabled || string.IsNullOrEmpty(name)) return;
        AudioManager.Stop(name);
    }

    public static bool TryGetTargetWorldAnchor(Transform target, out Vector3 worldAnchor, Transform explicitAnchor = null, bool searchChildNamedLockAnchor = true, bool preferRendererOverCollider = true)
    {
        worldAnchor = Vector3.zero;
        if (!target) return false;

        if (explicitAnchor)
        {
            worldAnchor = explicitAnchor.position;
            return true;
        }

        if (searchChildNamedLockAnchor)
        {
            var lockAnchor = FindChildByNameRecursive(target, "LockAnchor");
            if (lockAnchor)
            {
                worldAnchor = lockAnchor.position;
                return true;
            }
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        Collider[] colliders = target.GetComponentsInChildren<Collider>();

        bool hasRenderer = renderers != null && renderers.Length > 0;
        bool hasCollider = colliders != null && colliders.Length > 0;

        if (preferRendererOverCollider && hasRenderer)
        {
            var b = new Bounds(renderers[0].bounds.center, Vector3.zero);
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            worldAnchor = b.center;
            return true;
        }

        if (hasCollider)
        {
            var b = new Bounds(colliders[0].bounds.center, Vector3.zero);
            for (int i = 1; i < colliders.Length; i++) b.Encapsulate(colliders[i].bounds);
            worldAnchor = b.center;
            return true;
        }

        if (!preferRendererOverCollider && hasRenderer)
        {
            var b = new Bounds(renderers[0].bounds.center, Vector3.zero);
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            worldAnchor = b.center;
            return true;
        }

        worldAnchor = target.position;
        return true;
    }

    private static void CacheTargetMetrics(Camera cam, Transform t, ref Vector3 lastDir, ref float lastDist)
    {
        if (!cam || !t) { lastDir = Vector3.forward; lastDist = 0f; return; }
        Vector3 v = t.position - cam.transform.position;
        lastDist = v.magnitude;
        lastDir = lastDist > 0f ? v / lastDist : Vector3.forward;
    }

    private static bool IsChildOf(Transform child, Transform potentialParent)
    {
        var p = child;
        while (p) { if (p == potentialParent) return true; p = p.parent; }
        return false;
    }

    private static Transform FindChildByNameRecursive(Transform root, string name)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            var r = FindChildByNameRecursive(c, name);
            if (r) return r;
        }
        return null;
    }
}