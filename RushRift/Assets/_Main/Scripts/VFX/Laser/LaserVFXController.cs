using UnityEngine;

namespace Game.VFX
{
    public class LaserVFXController : MonoBehaviour
    {
        public void SetEndPos(Vector3 endPos)
        {
            Debug.Log($"Set Pos to {endPos}");
        }
    }
}