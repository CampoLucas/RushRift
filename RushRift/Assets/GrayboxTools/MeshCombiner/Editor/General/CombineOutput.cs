using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrayboxTools.MeshCombiner.Editor
{
    public sealed class CombineOutput : IDisposable
    {
        public Mesh Mesh;
        public Material[] Materials;
        public readonly List<Mesh> TempMeshes = new();
        
        public void Dispose()
        {
            for (var i = 0; i < TempMeshes.Count; i++)
            {
                if (!TempMeshes[i].IsNullOrMissing()) UnityEngine.Object.DestroyImmediate(TempMeshes[i]);
            }
            
            TempMeshes.Clear();
        }
    }
}