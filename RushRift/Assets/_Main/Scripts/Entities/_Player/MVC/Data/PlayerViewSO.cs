using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Player/View")]
    public class PlayerViewSO : EntityViewSO
    {
        public override NullCheck<IView> GetProxy()
        {
            return new NullCheck<IView>(new EntityView<PlayerViewSO>(this));
        }
    }
}