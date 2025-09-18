using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.LevelElements.Terminal
{
    [DisallowMultipleComponent]
    public class Terminal : MonoBehaviour, ISubject<string>
    {
        public enum SendBehavior { Toggle, AlwaysSendOn, AlwaysSendOff }

        public static readonly string ON_ARGUMENT = "on";
        public static readonly string OFF_ARGUMENT = "off";

        [Header("Settings")]
        [SerializeField, Tooltip("Initial state when the scene starts.")]
        private bool startsOn = false;

        [SerializeField, Tooltip("If enabled, this terminal can be used only once.")]
        private bool onlyUseOnce = false;

        [SerializeField, Tooltip("What this terminal sends when used.")]
        private SendBehavior sendBehavior = SendBehavior.Toggle;

        [Header("Observers")]
        [SerializeField, Tooltip("Targets that receive ON/OFF notifications.")]
        private ObserverComponent[] observers;

        [Header("Feedbacks")] 
        [SerializeField] private FloatingTextFeedback floatingTextFeedback;
        [SerializeField] private FlickerPlayer flickerPlayer;
        
        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed logs.")]
        private bool isDebugLoggingEnabled = false;

        [SerializeField, Tooltip("Draw gizmos for the terminal state.")]
        private bool drawGizmos = true;

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
            Log($"Awake state={_state}");
        }

        public void Do()
        {
            if (!LevelManager.CanUseTerminal)
            {
                Log("Use ignored: terminals are disabled");
                return;
            }

            if (onlyUseOnce && _usedOnce)
            {
                Log("Use ignored: OnlyUseOnce");
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
            
            Log($"Notify {arg.ToUpper()}");
            NotifyAll(arg);

            if (onlyUseOnce) _usedOnce = true;
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

        private void Log(string msg)
        {
            if (!isDebugLoggingEnabled) return;
            Debug.Log($"[Terminal] {name}: {msg}", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            bool stateVisual = Application.isPlaying ? _state : startsOn;
            Gizmos.color = stateVisual ? gizmoColorOn : gizmoColorOff;

            var p = transform.position;
            float r = 0.25f;

            Gizmos.DrawSphere(p, r * 0.5f);
            Gizmos.DrawWireSphere(p, r);

            var up = Vector3.up * 0.5f;
            Gizmos.DrawLine(p, p + (stateVisual ? up : -up));
        }
#endif
    }
}
