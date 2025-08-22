using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.LevelElements.Terminal
{
    public class Terminal : MonoBehaviour, ISubject<string>
    {
        public static readonly string ON_ARGUMENT = "on";
        public static readonly string OFF_ARGUMENT = "off";
        
        [Header("Settings")]
        [SerializeField] private bool startsOn;
        
        [Header("Observers")]
        [SerializeField] private ObserverComponent[] observers;

        private ISubject<string> _subject = new Subject<string>();
        private bool _state;
        //private bool _prevState;

        private void Awake()
        {
            for (var i = 0; i < observers.Length; i++)
            {
                var o = observers[i];
                if (!o) continue;

                _subject.Attach(o);
            }

            _state = startsOn;
            //_prevState = !_state;
        }

        public void Do()
        {
            if (_state) NotifyAll(ON_ARGUMENT);
            else NotifyAll(OFF_ARGUMENT);
            _state = !_state;
        }

        public bool Attach(IObserver<string> observer) => _subject.Attach(observer);
        public bool Detach(IObserver<string> observer) => _subject.Detach(observer);
        public void DetachAll() => _subject.DetachAll();

        public void NotifyAll(string arg)
        {
            if (!LevelManager.CanUseTerminal) return;
            
            _subject.NotifyAll(arg);
        }
        
        public void Dispose()
        {
            observers = null;
            _subject.DetachAll();
            _subject.Dispose();

            _subject = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }

    // public enum Mechanism // made into classes instead of enums (TerminalMomentary, TerminalToggle, TerminalLatch, TerminalTimed)
    // {
    //     Momentary, // Only active while pressed (pressure plates, etc) Push button, Tactile switch, Membrane button
    //     Toggle, // Toggle switch (SPST/SPDT), Rocker switch, Slide switch, Rotary switch
    //     Latch, // Push-on/push-off, Key switch, Push-pull latch
    //     Timed // Momentary push with delay, Double-action switch, Interlock switch, Dead-man switch
    // }
}