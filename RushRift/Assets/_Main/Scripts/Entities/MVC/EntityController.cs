using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Entities
{
    /// <summary>
    /// The main MonoBehaviour that wires the Model and View at runtime
    /// </summary>
    public class EntityController : ObserverComponent, IController
    {
        public static string DESTROY = "destroy";
        /// <summary>
        /// Reference to the GameObject's transform
        /// </summary>
        public Transform Origin { get; private set; }
        /// <summary>
        /// Collections of transforms that represents the entity joints
        /// </summary>
        public Joints<EntityJoint> Joints => joints;

        [Header("Data")]
        [SerializeField] protected EntityModelSO model; // ScriptableObject used to create the model proxy // ScriptableObject used to create the view proxy

        [Header("References")]
        [SerializeField] protected Joints<EntityJoint> joints;
        [SerializeField] protected Animator[] animator;

        protected EntityStateMachine _fsm; // Optional state machine for entity behavior
        
        private IModel _model; // Runtime model proxy instance
        private IView _view; // Runtime view proxy instance

        private List<string> _observersList = new();
        private List<string> _subjectsList = new();
        private Dictionary<string, IObserver> _observersDict = new();
        private Dictionary<string, ISubject> _subjectsDict = new();

        protected virtual void Awake()
        {
            Origin = transform;

            SetJoins();
            
            // Set observers
            AddObserver(DESTROY, new ActionObserver(DestroyEntity));
            
            // Create the model proxy and initialize it
            if (model.GetProxy().TryGetValue(out _model))
            {
                _model.Init(this);
            }

            // Create the view proxy and initialize it
            if (TryGetComponent<IView>(out _view))
            {
                _view.Init(animator);
            }
        }

        protected virtual void Start()
        {
            InitStateMachine();
        }

        protected virtual void Update()
        {
            var delta = Time.deltaTime;
            if (_fsm != null) _fsm.Run(delta);
            _model.Update(delta);
        }

        protected void LateUpdate()
        {
            var delta = Time.deltaTime;
            _model.LateUpdate(delta);
        }

        protected void FixedUpdate()
        {
            var delta = Time.fixedDeltaTime;
            _model.FixedUpdate(delta);
        }

        protected virtual void SetJoins()
        {
            
        }

        /// <summary>
        /// Exposes the runtime model instance
        /// </summary>
        /// <returns></returns>
        public IModel GetModel() => _model;
        /// <summary>
        /// Exposes the runtime view instance
        /// </summary>
        /// <returns></returns>
        public IView GetView() => _view;

        /// <summary>
        /// Used to provide input direction (must be implemented by subclasses)
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 MoveDirection() => Vector3.zero;
        public bool TryGetObserver(string key, out IObserver observer) => _observersDict.TryGetValue(key, out observer);
        public bool TryGetSubject(string key, out ISubject subject) => _subjectsDict.TryGetValue(key, out subject);
        
        public void OnDrawGizmos()
        {
            if (_model != null) _model.OnDraw(transform);
        }

        public void OnDrawGizmosSelected()
        {
            if (_model != null) _model.OnDrawSelected(transform);
        }

        public override void OnNotify(string arg)
        {
            if (_observersDict.TryGetValue(arg, out var observer))
            {
                observer.OnNotify();
            }
        }

        /// <summary>
        /// Cleans up model, view, FSM and all coroutines.
        /// </summary>
        public override void Dispose()
        {
            model = null;
            if (_model != null) _model.Dispose();
            _model = null;
            
            //if (_view != null) _view.Dispose();
            _view = null;
            
            if (_fsm != null) _fsm.Dispose();
            _fsm = null;

            for (var i = 0; i < _observersList.Count; i++)
            {
                _observersDict[_observersList[i]].Dispose();
            }
            
            for (var i = 0; i < _subjectsList.Count; i++)
            {
                _subjectsDict[_subjectsList[i]].Dispose();
            }
            
            _observersList.Clear();
            _observersDict.Clear();
            _subjectsList.Clear();
            _subjectsDict.Clear();

            StopAllCoroutines();
            
            OnDispose();
        }

        public void OnDestroy()
        {
            Dispose();
        }
        
        /// <summary>
        /// Optional override to setup the FSM
        /// </summary>
        protected virtual void InitStateMachine() { }
        
        /// <summary>
        /// Hook for subclass specific disposal
        /// </summary>
        protected virtual void OnDispose() { }

        protected bool AddObserver(string key, IObserver observer, bool discardOnFail = true)
        {
            if (!_observersDict.TryAdd(key, observer))
            {
                if (discardOnFail) observer.Dispose();
                return false;
            }
            
            _observersList.Add(key);
            return true;
        }
        
        protected bool RemoveObserver(string key, IObserver observer, bool discardOnRemove = true)
        {
            if (!_observersDict.Remove(key, out var o))
            {
                return false;
            }

            _observersList.Remove(key);
            if (discardOnRemove) o.Dispose();
            return true;
        }
        
        protected bool AddSubject(string key, ISubject subject, bool discardOnFail = true)
        {
            if (!_subjectsDict.TryAdd(key, subject))
            {
                if (discardOnFail) subject.Dispose();
                return false;
            }

            _subjectsList.Add(key);
            return true;
        }
        
        protected bool RemoveSubject(string key, IObserver observer, bool discardOnRemove = true)
        {
            if (!_subjectsDict.Remove(key, out var o))
            {
                return false;
            }

            _subjectsList.Remove(key);
            if (discardOnRemove) o.Dispose();
            return true;
        }

        private void DestroyEntity()
        {
            Destroy(gameObject);
        }
    }
}
