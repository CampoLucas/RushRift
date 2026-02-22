using System;
using System.Collections.Generic;
using Game.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tools.MeshCombiner.Editor
{
    public enum McMaterial {
        First, PreserveAll, SkipDuplicates
    }

    public enum McPivotApply
    {
        MoveToPivot, // world stays the same
        KeepInPlace, // Mesh moves so pivot ends up at target
        //SnapToParent // Mesh moves so pivot ends up at parent origin/rot
    }
    
    public static class MeshCombinerTool
    {
        public enum CombineResult
        {
            Successful,
            MissingReference,
            OutOfRangeException
        }
        private struct WorldXForm
        {
            public Transform t;
            public Transform parent;
            public Matrix4x4 world;
        }
        
        public struct CombineArguments
        {
            public string MeshName;
            
            public List<MeshFilter> MeshFilters;
            public Func<Vector3> GetPosition;
            public Func<Quaternion> GetRotation;

            public McPivotApply PivotApply;
            public McMaterial MaterialOption;
        }
        
        public static CombineResult TryCombineMesh(ref MeshFilter meshFilter, ref MeshRenderer meshRenderer, CombineArguments args, Action onCombined = null)
        {
            if (args.MeshFilters == null || args.MeshFilters.Count == 0)
            {
                return Fail(CombineResult.MissingReference, "Trying to combine without meshes");
            }

            meshFilter ??= args.MeshFilters[0];
            meshRenderer ??= meshFilter.GetComponent<MeshRenderer>();

            if (meshRenderer.IsNullOrMissing())
            {
                return Fail(CombineResult.MissingReference, "Target MeshRenderer is null or missing. Please assign the Target MeshRenderer.");
            }

            var pivotPos = args.GetPosition();
            var pivotRot = args.GetRotation();

            // Bake vertices into pivot-local space.
            var pivotWorldToLocal = Matrix4x4.TRS(pivotPos, pivotRot, Vector3.one).inverse;
            
            switch (args.PivotApply)
            {
                case McPivotApply.MoveToPivot:
                {
                    Undo.RecordObject(meshFilter.transform, "Move Combined Mesh To Pivot");
                    var t = meshFilter.transform;
                    //t.SetPositionAndRotation(pivotPos, pivotRot);
                    MoveTransformPivot(t, pivotPos, pivotRot);
                    break;
                }
                case McPivotApply.KeepInPlace:
                    break;
                // case McPivotApply.SnapToParent:
                // {
                //     var t = meshFilter.transform;
                //     var parent = t.parent;
                //     
                //     var snapPos = parent ? parent.position : Vector3.zero;
                //     var snapRot = parent ? parent.rotation : Quaternion.identity;
                //     Undo.RecordObject(t, "Snap Combined Mesh To Parent");
                //     //MoveTransformPivot(t, snapPos, snapRot);
                //     t.SetPositionAndRotation(snapPos, snapRot);
                //     break;
                // }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // Prealloc
            var estimatedSubmeshes = 0;
            foreach (var filter in args.MeshFilters)
            {
                if (!filter || !filter.sharedMesh || !filter.TryGetComponent<MeshRenderer>(out var renderer)) continue;

                estimatedSubmeshes += Mathf.Min(filter.sharedMesh.subMeshCount, renderer.sharedMaterials?.Length ?? 0);
            }

            if (estimatedSubmeshes < 1) estimatedSubmeshes = args.MeshFilters.Count;
            
            // Build combines + material
            var combineList = new List<CombineInstance>(estimatedSubmeshes);
            var materialsList = new List<Material>(estimatedSubmeshes);
            
            // SkipDuplicates mode
            Dictionary<Material, List<CombineInstance>> materialDict = null;
            if (args.MaterialOption == McMaterial.SkipDuplicates)
                materialDict = new Dictionary<Material, List<CombineInstance>>(Mathf.Min(estimatedSubmeshes, 128));

            Material firstMaterial = null;

            foreach (var filter in args.MeshFilters)
            {
                if (!filter) continue;

                var mesh = filter.sharedMesh;
                if (!mesh) continue;

                if (!filter.TryGetComponent<MeshRenderer>(out var renderer))
                {
                    Debug.LogWarning($"MeshRenderer missing on '{filter.name}', skipping.");
                    continue;
                }

                var sharedMats = renderer.sharedMaterials;
                if (sharedMats == null || sharedMats.Length == 0)
                {
                    Debug.LogWarning($"No materials on '{filter.name}', skipping.");
                    continue;
                }

                firstMaterial ??= sharedMats[0];
                
                // Each submesh corresponds to one material slot
                var subMeshCount = Mathf.Min(mesh.subMeshCount, sharedMats.Length);
                
                // Convert object into pivot-local space
                var toPivotLocal = pivotWorldToLocal * filter.transform.localToWorldMatrix;

                for (var i = 0; i < subMeshCount; i++)
                {
                    var mat = sharedMats[i];
                    if (!mat) continue;
                    
                    var combined = new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = i,
                        transform = toPivotLocal
                    };

                    switch (args.MaterialOption)
                    {
                        case McMaterial.First:
                        {
                            // If first, skip submeshes/materials
                            combineList.Add(combined);
                            break;
                        }
                        case McMaterial.PreserveAll:
                        {
                            combineList.Add(combined);
                            materialsList.Add(mat);
                            break;
                        }
                        case McMaterial.SkipDuplicates:
                        {
                            if (!materialDict.TryGetValue(mat, out var list))
                            {
                                list = new List<CombineInstance>(16);
                                materialDict.Add(mat, list);
                            }
                            list.Add(combined);
                            break;
                        }
                            
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                
                filter.gameObject.SetActive(false);
            }

            var result = CombineResult.Successful;
            switch (args.MaterialOption)
            {
                case McMaterial.First:
                    result = CombineFirst(args.MeshName, ref meshFilter, ref meshRenderer, combineList, firstMaterial);
                    break;
                case McMaterial.PreserveAll:
                    result = CombinePreserveAll(args.MeshName, ref meshFilter, ref meshRenderer, combineList, materialsList);
                    break;
                case McMaterial.SkipDuplicates:
                    result = CombineSkipDuplicates(args.MeshName, ref meshFilter, ref meshRenderer, materialDict);
                    break;
                default:
                    result = Fail(CombineResult.OutOfRangeException, "Combine Material Option is out of exception.");
                    throw new ArgumentOutOfRangeException();
            }

            if (result != CombineResult.Successful) return result;

            if (onCombined != null) onCombined();
            return CombineResult.Successful;
        }
        
        private static CombineResult CombineFirst(string meshName, ref MeshFilter filter, ref MeshRenderer renderer, List<CombineInstance> combineList, Material firstMaterial)
        {
            if (combineList.Count == 0)
            {
                return Fail(CombineResult.MissingReference, "Nothing to combine. Please assign the MeshFilters to combine.");
            }

            var outMesh = new Mesh
            {
                name = meshName
            };
            outMesh.CombineMeshes(combineList.ToArray(), mergeSubMeshes: true, useMatrices: true);

            filter.sharedMesh = outMesh;
            renderer.sharedMaterials = firstMaterial ? new[] { firstMaterial } : renderer.sharedMaterials;
            return CombineResult.Successful;
        }

        private static CombineResult CombinePreserveAll(string meshName, ref MeshFilter filter, ref MeshRenderer renderer, List<CombineInstance> combineList, List<Material> materialsList)
        {
            if (combineList.Count == 0)
            {
                return Fail(CombineResult.MissingReference, "Nothing to combine. Please assign the MeshFilters to combine.");
            }

            var outMesh = new Mesh { name = meshName };
            // mergeSubMeshes = false is the key: we keep one submesh per CombineInstance
            outMesh.CombineMeshes(combineList.ToArray(), mergeSubMeshes: false, useMatrices: true);

            filter.sharedMesh = outMesh;
            renderer.sharedMaterials = materialsList.ToArray();
            return CombineResult.Successful;
        }
        
        private static CombineResult CombineSkipDuplicates(string meshName, ref MeshFilter filter, ref MeshRenderer renderer, Dictionary<Material,List<CombineInstance>> byMaterial)
        {
            if (byMaterial == null || byMaterial.Count == 0)
            {
                return Fail(CombineResult.MissingReference, "Nothing to combine. Please assign the MeshFilters to combine.");
            }

            var tempMeshes = new List<Mesh>(byMaterial.Count);
            var finalCombines = new List<CombineInstance>(byMaterial.Count);
            var finalMats = new List<Material>(byMaterial.Count);

            foreach (var kv in byMaterial)
            {
                var mat = kv.Key;
                var list = kv.Value;
                if (mat == null || list == null || list.Count == 0)
                    continue;

                // combine everything that uses this material into a single mesh
                var m = new Mesh { name = $"_tmp_{mat.name}" };
                m.CombineMeshes(list.ToArray(), mergeSubMeshes: true, useMatrices: true);

                tempMeshes.Add(m);
                finalMats.Add(mat);

                // combine material-meshes into the final mesh as separate submeshes
                finalCombines.Add(new CombineInstance
                {
                    mesh = m,
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity
                });
            }

            var outMesh = new Mesh
            {
                name = meshName
            };
            outMesh.CombineMeshes(finalCombines.ToArray(), mergeSubMeshes: false, useMatrices: true);

            filter.sharedMesh = outMesh;
            renderer.sharedMaterials = finalMats.ToArray();

            // Cleanup temp meshes
            foreach (var tm in tempMeshes)
                Object.DestroyImmediate(tm);

            return CombineResult.Successful;
        }
        
        private static CombineResult Fail(CombineResult code, string msg)
        {
            EditorUtility.DisplayDialog("Target MeshRenderer is missing", msg, "OK");
            return code;
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