using UnityEngine;

namespace Game.Entities
{
    public class JointsContainer : MonoBehaviour
    {
        public Joints<EntityJoint> Joints => joints;
        
        [SerializeField] private Joints<EntityJoint> joints;
    }
}