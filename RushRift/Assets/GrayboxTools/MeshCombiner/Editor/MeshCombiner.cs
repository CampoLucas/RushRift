using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrayboxTools.MeshCombiner.Editor
{
    public static class MeshCombiner
    {
        private struct WorldXForm
        {
            public Transform t;
            public Transform parent;
            public Matrix4x4 world;
        }

        public static bool TryCombineToTarget(MeshFilter targetFilter, MeshRenderer targetRenderer,
            in CombineRequest input, McPivotApply pivotApply, bool disableSources, out string error)
        {
            error = null;

            var filterMissing = targetFilter.IsNullOrMissing();
            var rendererMissing = targetRenderer.IsNullOrMissing();
            
            if (filterMissing || rendererMissing)
            {
                var parts = new List<string>();
                if (filterMissing) parts.Add("MeshFilter");
                if (rendererMissing) parts.Add("MeshRenderer");
                var missingComp = string.Join(" and ", parts);
                
                error = $"Target {missingComp} missing.";
                return false;
            }
            
            // pivot apply
            ApplyPivot(pivotApply, targetFilter.transform, input.WorldPos, input.WorldRot);

            if (!TryBuild(input, out var output, out error))
            {
                return false;
            }
            
            // assign the results
            targetFilter.sharedMesh = output.Mesh;
            targetRenderer.sharedMaterials = output.Materials;
            
            if (disableSources) DisableSources(input.Sources);
            output.Dispose(); // clean the temporary meshes from SkipDuplicates
            return true;
        }

        private static bool TryBuild(in CombineRequest input, out CombineOutput output, out string error)
        {
            output = null;
            error = null;

            var sources = input.Sources;
            if (sources == null)
            {
                error = "The MeshFilters to combine list is null.";
                return false;
            }
            
            var sCount = sources.Count;
            if (sCount == 0)
            {
                error = "No MeshFilters to combine.";
                return false;
            }
            
            // Bake vertices into pivot-local space.
            var worldToLocal = Matrix4x4.TRS(input.WorldPos, input.WorldRot, Vector3.one).inverse;
            
            // Pre allocate estimated
            var estimatedSubMeshes = 0;
            for (var i = 0; i < sCount; i++)
            {
                var filter = sources[i];
                if (filter.IsNullOrMissing() || !filter.sharedMesh ||
                    !filter.TryGetComponent<MeshRenderer>(out var renderer) ||
                    renderer.sharedMaterials.IsNullOrMissing())
                {
                    continue;
                }

                estimatedSubMeshes += Mathf.Min(filter.sharedMesh.subMeshCount, renderer.sharedMaterials.Length);
            }
            
            if (estimatedSubMeshes < 1) estimatedSubMeshes = sCount;

            var combineList = new List<CombineInstance>(estimatedSubMeshes);
            var materialsList = new List<Material>(estimatedSubMeshes);

            Dictionary<Material, List<CombineInstance>> byMat = null;
            if (input.MaterialMode == SubmeshOption.MergeByMaterial)
            {
                byMat = new Dictionary<Material, List<CombineInstance>>(Mathf.Min(estimatedSubMeshes, 128));
            }

            Material firstMaterial = null;

            for (var i = 0; i < sCount; i++)
            {
                var filter = sources[i];
                if (filter.IsNullOrMissing() || !filter.sharedMesh || !filter.TryGetComponent<MeshRenderer>(out var renderer) ||
                    renderer.sharedMaterials.IsNullOrMissing() || renderer.sharedMaterials.Length == 0)
                {
                    continue;
                }

                var mesh = filter.sharedMesh;
                var sharedMats = renderer.sharedMaterials;
                
                firstMaterial ??= sharedMats[0];

                var subMeshCount = Mathf.Min(mesh.subMeshCount, sharedMats.Length);
                
                // Convert object to local space
                var toLocal = worldToLocal * filter.transform.localToWorldMatrix;

                for (var j = 0; j < subMeshCount; j++)
                {
                    var mat = sharedMats[j];
                    if (!mat) continue;

                    var ci = new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = j,
                        transform = toLocal
                    };

                    switch (input.MaterialMode)
                    {
                        case SubmeshOption.MergeAllSubmeshes:
                            combineList.Add(ci);
                            break;

                        case SubmeshOption.KeepAllSubmeshes:
                            combineList.Add(ci);
                            materialsList.Add(mat);
                            break;

                        case SubmeshOption.MergeByMaterial:
                            if (byMat == null)
                            {
                                error = "The material dictionary is null.";
                                return false;
                            }
                            
                            if (!byMat.TryGetValue(mat, out var list))
                            {
                                list = new List<CombineInstance>(16);
                                byMat.Add(mat, list);
                            }
                            list.Add(ci);
                            break;
                    }
                }
            }

            if (input.MaterialMode != SubmeshOption.MergeByMaterial && combineList.Count == 0)
            {
                error = "Nothing to combine.";
                return false;
            }
            if (input.MaterialMode == SubmeshOption.MergeByMaterial && (byMat == null || byMat.Count == 0))
            {
                error = "Nothing to combine.";
                return false;
            }

            output = new CombineOutput();

            switch (input.MaterialMode)
            {
                case SubmeshOption.MergeAllSubmeshes:
                {
                    var outMesh = new Mesh
                    {
                        name = input.MeshName
                    };
                    
                    outMesh.CombineMeshes(combineList.ToArray(), mergeSubMeshes: true, useMatrices: true);
                    output.Mesh = outMesh;
                    output.Materials = firstMaterial ? new[] { firstMaterial } : Array.Empty<Material>();

                    return true;
                }
                case SubmeshOption.KeepAllSubmeshes:
                {
                    var outMesh = new Mesh
                    {
                        name = input.MeshName
                    };
                    
                    outMesh.CombineMeshes(combineList.ToArray(), mergeSubMeshes: false, useMatrices: true);
                    output.Mesh = outMesh;
                    output.Materials = materialsList.ToArray();
                    return true;
                }
                case SubmeshOption.MergeByMaterial:
                {
                    if (byMat == null)
                    {
                        error = "The material dictionary is null.";
                        return false;
                    }
                    var finalInstances = new List<CombineInstance>(byMat.Count);
                    var finalMaterials = new List<Material>(byMat.Count);

                    foreach (var kv in byMat)
                    {
                        var mat = kv.Key;
                        var list = kv.Value;
                        
                        if (!mat || list == null || list.Count == 0) continue;

                        var tmp = new Mesh
                        {
                            name = $"_tmp_{mat.name}"
                        };
                        tmp.CombineMeshes(list.ToArray(), mergeSubMeshes: true, useMatrices: true);
                        
                        output.TempMeshes.Add(tmp);
                        finalMaterials.Add(mat);
                        
                        finalInstances.Add(new CombineInstance
                        {
                            mesh = tmp,
                            subMeshIndex = 0,
                            transform = Matrix4x4.identity
                        });
                    }

                    var outMesh = new Mesh
                    {
                        name = input.MeshName
                    };
                    
                    outMesh.CombineMeshes(finalInstances.ToArray(), mergeSubMeshes: false, useMatrices: true);

                    output.Mesh = outMesh;
                    output.Materials = finalMaterials.ToArray();
                    return true;
                }
                default:
                {
                    error = "Invalid material mode.";
                    output.Dispose();
                    output = null;
                    return false;
                }
            }
        }

        private static void ApplyPivot(McPivotApply mode, Transform target, Vector3 pos, Quaternion rot)
        {
            if (!target)
            {
                return;
            }

            switch (mode)
            {
                case McPivotApply.MoveToPivot:
                {
                    Undo.RecordObject(target, "Move Combined Mesh To Pivot");
                    MoveTransformPivot(target, pos, rot);
                    break;
                }
            }
        }

        private static void DisableSources(IReadOnlyList<MeshFilter> sources)
        {
            if (sources == null || sources.Count == 0) return;
            for (var i = 0; i < sources.Count; i++)
            {
                var mf = sources[i];
                if (mf) mf.gameObject.SetActive(false);
            }
        }

        private static void MoveTransformPivot(Transform root, Vector3 worldPos, Quaternion worldRot)
        {
            // Cache all descendants world matrices
            var list = new List<WorldXForm>();
            var stack = new Stack<Transform>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                for (var i = 0; i < cur.childCount; i++)
                {
                    var c = cur.GetChild(i);
                    list.Add(new WorldXForm
                    {
                        t = c,
                        parent = c.parent,
                        world = c.localToWorldMatrix
                    });
                    stack.Push(c);
                }
            }
            
            // Move the root
            root.SetPositionAndRotation(worldPos, worldRot);
            
            // Restore descendatns locals from cached world
            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (!w.t) continue;

                var parentWorldToLocal = w.parent ? w.parent.worldToLocalMatrix : Matrix4x4.identity;
                var local = parentWorldToLocal * w.world;

                if (!TryDecomposeTRS(local, out var lp, out var lr, out var ls))
                {
                    // Fallback
                    w.t.position = w.world.GetColumn(3);
                    w.t.rotation = w.world.rotation;
                    continue;
                }
                
                w.t.localPosition = lp;
                w.t.localRotation = lr;
                w.t.localScale = ls;
            }
        }

        private static bool TryDecomposeTRS(Matrix4x4 m, out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            pos = new Vector3(m.m03, m.m13, m.m23);
            
            var x = new Vector3(m.m00, m.m10, m.m20);
            var y = new Vector3(m.m01, m.m11, m.m21);
            var z = new Vector3(m.m02, m.m12, m.m22);

            scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);

            if (scale.x < 1e-8f || scale.y < 1e-8f || scale.z < 1e-8f)
            {
                rot = Quaternion.identity;
                return false;
            }

            var xn = x / scale.x;
            var yn = y / scale.y;
            var zn = z / scale.z;

            // Build rotation from normalized axes
            var r = Matrix4x4.identity;
            r.m00 = xn.x; r.m10 = xn.y; r.m20 = xn.z;
            r.m01 = yn.x; r.m11 = yn.y; r.m21 = yn.z;
            r.m02 = zn.x; r.m12 = zn.y; r.m22 = zn.z;

            rot = Quaternion.LookRotation(r.GetColumn(2), r.GetColumn(1));
            return true;
        }
    }
}