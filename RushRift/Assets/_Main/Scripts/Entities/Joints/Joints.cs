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

            return j ? j : defaultJoint;
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