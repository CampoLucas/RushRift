using System;
using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public class TriggerCollection : SubjectCollection<ITrigger>, ITrigger
    {
        public TriggerCollection() : base(false, false, true)
        {
            
        }
        
        public bool Evaluate(ref IController controller)
        {
            var result = false;
            foreach (var subject in Subjects)
            {
                if (!subject.Evaluate(ref controller)) continue;
                result = true;
                break;
            }

            return result;
        }
    }
}