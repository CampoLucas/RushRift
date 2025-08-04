using System;
using Game.Tools;
using UnityEngine;

namespace Game.Entities
{
    [System.Serializable]
    public class Joints<T> where T : Enum
    {
        [SerializeField] private Transform defaultJoint;
        [SerializeField] private EnumContainer<T, Transform> jointMap;

        public Transform GetJoint(T joint)
        {
            var j = jointMap[joint];

            //return j ? j : defaultJoint;
            if (j)
            {
                return j;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"WARNING: The requested joint is null, returning the default joint {defaultJoint.gameObject.name}");
#endif
                return defaultJoint;
            }
        }

        public void SetJoint(T joint, Transform value)
        {
            jointMap[joint] = value;
        }

        public Transform[] GetJoints()
        {
            return jointMap.GetContent();
        }

        public void AddJoint(Joints<T> joints)
        {
            var jointTransforms = joints.GetJoints();

            for (var i = 0; i < jointTransforms.Length; i++)
            {
                var joint = jointTransforms[i];
                if (!joint) continue;
                
                jointMap[i] = joint;
            }
        }
    }
}