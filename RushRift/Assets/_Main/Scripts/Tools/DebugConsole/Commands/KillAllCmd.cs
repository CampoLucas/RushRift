using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Components;
using MyTools.Global;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    public class KillAllCmd : DebugCommand
    {
        private bool _dashHack;
        
        public KillAllCmd() : base("kill_all", "Removes all the enemies and traps from the level", "kill_all")
        {
            _command = KillAllCommand;
        }
        
        private bool KillAllCommand()
        {
            var controllers = new List<IController>();
            controllers.AddRange(Object.FindObjectsOfType<EnemyController>());
            controllers.AddRange(Object.FindObjectsOfType<LaserController>());

            for (var i = 0; i < controllers.Count; i++)
            {
                var e = controllers[i];
                    
                if (e == null) continue;
                var model = e.GetModel();
                    
                if (model == null) continue;
                if (model.TryGetComponent<DestroyableComponent>(out var destroyableComponent))
                {
                    destroyableComponent.DestroyEntity();
                }
                else if (model.TryGetComponent<HealthComponent>(out var healthComponent))
                {
                    healthComponent.Intakill(Vector3.zero);
                }
                else
                {
                    Debug.LogWarning($"Couldn't remove enemy {e.GetType()}");
                }
            }

            return true;
        }
    }
}