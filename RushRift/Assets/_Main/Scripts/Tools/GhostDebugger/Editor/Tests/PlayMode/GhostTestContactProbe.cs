using UnityEngine;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// Counts trigger and collision contacts coming from the ghost hierarchy.
    /// </summary>
    public class GhostTestContactProbe : MonoBehaviour
    {
        public Transform Ghost { get; set; }
        public int Contacts { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            Count(other.transform);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Count(collision.transform);
        }

        private void Count(Transform other)
        {
            if (Ghost != null && other.IsChildOf(Ghost))
            {
                Contacts++;
            }
        }
    }
}
