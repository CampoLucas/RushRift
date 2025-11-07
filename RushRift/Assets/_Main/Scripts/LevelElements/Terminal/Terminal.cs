using _Main.Scripts.Feedbacks;
using Game.DesignPatterns.Observers;
using MyTools.Global;
using UnityEngine;

namespace Game.LevelElements
{
    [DisallowMultipleComponent]
    public class Terminal : MonoBehaviour, ISubject<string>
    {
        public enum SendBehavior { Toggle, AlwaysSendOn, AlwaysSendOff }

        public static readonly string ON_ARGUMENT = "on";
        public static readonly string OFF_ARGUMENT = "off";

        [Header("Settings")]
        [SerializeField] private bool startsOn = false;
        [SerializeField] private bool onlyUseOnce = false;
        [SerializeField] private SendBehavior sendBehavior = SendBehavior.Toggle;

        [Header("Observers")]
        [SerializeField] private ObserverComponent[] observers;

        [Header("Feedbacks")] 
        [SerializeField] private FloatingTextFeedback floatingTextFeedback;
        [SerializeField] private FlickerPlayer flickerPlayer;

        [SerializeField, Tooltip("Gizmo color when OFF.")]
        private Color gizmoColorOff = new Color(1f, 0.6f, 0.2f, 0.9f);

        [SerializeField, Tooltip("Gizmo color when ON.")]
        private Color gizmoColorOn = new Color(0.2f, 1f, 0.6f, 0.9f);

        private ISubject<string> _subject = new Subject<string>();
        private bool _state;
        private bool _usedOnce;

        private void Awake()
        {
            if (observers != null)
            {
                for (int i = 0; i < observers.Length; i++)
                {
                    var o = observers[i];
                    if (o) _subject.Attach(o);
                }
            }

            _state = startsOn;
            _usedOnce = false;
        }

        public void Do()
        {
            if (!GlobalLevelManager.PowerSurge)
            {
                return;
            }

            if (onlyUseOnce && _usedOnce)
            {
                return;
            }

            string arg;
            switch (sendBehavior)
            {
                case SendBehavior.AlwaysSendOn:
                    _state = true;
                    arg = ON_ARGUMENT;
                    break;
                case SendBehavior.AlwaysSendOff:
                    _state = false;
                    arg = OFF_ARGUMENT;
                    break;
                default:
                    _state = !_state;
                    arg = _state ? ON_ARGUMENT : OFF_ARGUMENT;
                    break;
            }
            
            flickerPlayer.FlickerPlay();
            floatingTextFeedback.Play();
            
            this.Log($"Notify {arg.ToUpper()}");
            NotifyAll(arg);

            if (onlyUseOnce) _usedOnce = true;
        }

        public bool Attach(IObserver<string> observer, bool disposeOnDetach = false) => _subject.Attach(observer, disposeOnDetach);
        public bool Detach(IObserver<string> observer) => _subject.Detach(observer);
        public void DetachAll() => _subject.DetachAll();

        public void NotifyAll(string arg)
        {
            if (!GlobalLevelManager.PowerSurge) return;
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

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var stateVisual = Application.isPlaying ? _state : startsOn;
            Gizmos.color = stateVisual ? gizmoColorOn : gizmoColorOff;

            var p = transform.position;
            var r = 0.25f;

            Gizmos.DrawSphere(p, r * 0.5f);
            Gizmos.DrawWireSphere(p, r);

            var up = Vector3.up * 0.5f;
            Gizmos.DrawLine(p, p + (stateVisual ? up : -up));
        }
#endif
    }
}
