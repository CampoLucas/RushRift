using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Ghost
{
    public static class GhostPlaybackUtils
    {
        public enum PosInterp { Linear, CatmullRom }
        public enum RotMode { UseRecordedRotation, FaceVelocity, IgnoreRotation }

        public struct Frame
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;
        }

        public static float Duration(IReadOnlyList<Frame> frames, float recordedDuration)
        {
            if (frames == null || frames.Count == 0) return 0f;
            return Mathf.Max(recordedDuration, frames[frames.Count - 1].time);
        }

        public static int AdvanceFrameIndex(IReadOnlyList<Frame> frames, float t, int currentIndex)
        {
            int count = frames.Count;
            int idx = currentIndex;
            while (idx < count && frames[idx].time < t) idx++;
            return Mathf.Clamp(idx, 1, count - 1);
        }

        public static Vector3 SamplePosition(IReadOnlyList<Frame> frames, int i0, int i1, float t, PosInterp mode)
        {
            var f0 = frames[i0];
            var f1 = frames[i1];

            float span = Mathf.Max(1e-5f, f1.time - f0.time);
            float u = Mathf.Clamp01((t - f0.time) / span);

            if (mode == PosInterp.CatmullRom && frames.Count >= 4)
            {
                int im1 = Mathf.Max(0, i0 - 1);
                int i2  = Mathf.Min(frames.Count - 1, i1 + 1);

                var fm1 = frames[im1];
                var f2  = frames[i2];

                float t_1 = fm1.time; float t0 = f0.time; float t1 = f1.time; float t2 = f2.time;

                Vector3 p_1 = fm1.position;
                Vector3 p0  = f0.position;
                Vector3 p1  = f1.position;
                Vector3 p2  = f2.position;

                float m0Scale = (t1 - t_1) > 1e-5f ? 1f / (t1 - t_1) : 0f;
                float m1Scale = (t2 - t0)  > 1e-5f ? 1f / (t2 - t0)  : 0f;

                Vector3 m0 = (p1 - p_1) * m0Scale;
                Vector3 m1 = (p2 - p0)  * m1Scale;

                float u2 = u * u;
                float u3 = u2 * u;

                float h00 =  2f*u3 - 3f*u2 + 1f;
                float h10 =      u3 - 2f*u2 + u;
                float h01 = -2f*u3 + 3f*u2;
                float h11 =      u3 -     u2;

                return h00 * p0 + m0 * (h10 * span) + h01 * p1 + m1 * (h11 * span);
            }

            return Vector3.Lerp(f0.position, f1.position, u);
        }

        public static Quaternion SampleRotation(IReadOnlyList<Frame> frames, int i0, int i1, float t, RotMode mode, Vector3 posForVelocity, Vector3 prevSmoothedPos, float dt, float faceVelMinSpeed)
        {
            var f0 = frames[i0];
            var f1 = frames[i1];

            float span = Mathf.Max(1e-5f, f1.time - f0.time);
            float u = Mathf.Clamp01((t - f0.time) / span);

            switch (mode)
            {
                case RotMode.UseRecordedRotation:
                    return Quaternion.Slerp(f0.rotation, f1.rotation, u);

                case RotMode.FaceVelocity:
                {
                    Vector3 v = (f1.position - f0.position) / span;
                    if (dt > 0f)
                        v = (posForVelocity - prevSmoothedPos) / Mathf.Max(1e-5f, dt);

                    if (v.sqrMagnitude > faceVelMinSpeed * faceVelMinSpeed)
                        return Quaternion.LookRotation(v.normalized, Vector3.up);

                    return Quaternion.Slerp(f0.rotation, f1.rotation, u);
                }

                default:
                    return f1.rotation;
            }
        }

        public static Vector3 SmoothPosition(Vector3 current, Vector3 target, ref Vector3 vel, float smoothTime, float dt)
            => Vector3.SmoothDamp(current, target, ref vel, smoothTime, Mathf.Infinity, Mathf.Max(0f, dt));

        public static Quaternion SmoothRotation(Quaternion current, Quaternion target, float smoothTime, float dt)
        {
            float k = smoothTime > 0f ? 1f - Mathf.Exp(-Mathf.Max(0f, dt) / smoothTime) : 1f;
            return Quaternion.Slerp(current, target, Mathf.Clamp01(k));
        }
    }

    public sealed class ProximityFader
    {
        private readonly string _playerTag;
        private readonly float _min;
        private readonly float _max;
        private readonly float _alphaNear;
        private readonly float _alphaFar;
        private readonly Material _mat;
        private Transform _player;
        private int _baseId, _colorId;
        private bool _ids;

        public ProximityFader(Material mat, string playerTag, float minDist, float maxDist, float alphaNear, float alphaFar)
        {
            _mat = mat; _playerTag = playerTag; _min = minDist; _max = maxDist; _alphaNear = alphaNear; _alphaFar = alphaFar;
        }

        public void Update(Vector3 ghostPos)
        {
            if (_mat == null) return;

            if (!_ids) { _baseId = Shader.PropertyToID("_BaseColor"); _colorId = Shader.PropertyToID("_Color"); _ids = true; }
            if (_player == null)
            {
                var go = GameObject.FindGameObjectWithTag(string.IsNullOrEmpty(_playerTag) ? "Player" : _playerTag);
                if (go) _player = go.transform;
                if (_player == null) return;
            }

            float dist = Vector3.Distance(ghostPos, _player.position);
            float t = Mathf.InverseLerp(_min, _max, dist);
            float a = Mathf.Lerp(_alphaNear, _alphaFar, t);

            if (_mat.HasProperty(_baseId)) { var c = _mat.GetColor(_baseId); c.a = a; _mat.SetColor(_baseId, c); }
            else if (_mat.HasProperty(_colorId)) { var c = _mat.GetColor(_colorId); c.a = a; _mat.SetColor(_colorId, c); }
        }
    }
}