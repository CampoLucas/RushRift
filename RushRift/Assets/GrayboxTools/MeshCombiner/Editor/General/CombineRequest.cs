using System.Collections.Generic;
using UnityEngine;

namespace GrayboxTools.MeshCombiner.Editor
{
    public readonly struct CombineRequest
    {
        public readonly string MeshName { get; }
        public readonly IReadOnlyList<MeshFilter> Sources { get; }
        public readonly Vector3 WorldPos { get; }
        public readonly Quaternion WorldRot { get; }
        public readonly SubmeshOption MaterialMode { get; }

        public CombineRequest(string meshName, IReadOnlyList<MeshFilter> sources, Vector3 worldPos, Quaternion worldRot, SubmeshOption matMode)
        {
            MeshName = meshName;
            Sources = sources;
            WorldPos = worldPos;
            WorldRot = worldRot;
            MaterialMode = matMode;
        }
    }
}