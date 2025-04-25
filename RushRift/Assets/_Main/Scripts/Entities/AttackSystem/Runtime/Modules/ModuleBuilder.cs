using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Factory;
using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    public abstract class ModuleBuilder<T> : StaticModuleData
        where T : RuntimeModuleData
    {
        public abstract T GetModuleData();

        public override IModuleData Test()
        {
            return GetModuleData();
        }
    }
}
