using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class DashModule : StaticModuleData
    {
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return new DashProxy(this, ChildrenProxies(controller), controller);
        }
    }
}
