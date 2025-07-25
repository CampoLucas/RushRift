using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Entities
{
    /// <summary>
    /// The main MonoBehaviour that wires the Model and View at runtime
    /// </summary>
    public abstract class EntityController : MonoBehaviour, IController
    {
        /// <summary>
        /// Reference to the GameObject's transform
        /// </summary>
        public Transform Origin { get; private set; }
        public VisualEffect SpeedLines => speedLines;
        /// <summary>
        /// Collections of transforms that represents the entity joints
        /// </summary>
        public Joints<EntityJoint> Joints => joints;

        [Header("Data")]
        [SerializeField] protected EntityModelSO model; // ScriptableObject used to create the model proxy
        [SerializeField] protected EntityViewSO view; // ScriptableObject used to create the view proxy

        [Header("References")]
        [SerializeField] protected Joints<EntityJoint> joints;
        [SerializeField] private Animator[] animator;
        [SerializeField] private VisualEffect speedLines;

        protected EntityStateMachine _fsm; // Optional state machine for entity behavior
        
        private IModel _model; // Runtime model proxy instance
        private IView _view; // Runtime view proxy instance

        protected virtual void Awake()
        {
            Origin = transform;

            SetJoins();
            
            // Create the model proxy and initialize it
            if (model.GetProxy().TryGetValue(out _model))
            {
                _model.Init(this);
            }

            // Create the view proxy and initialize it
            if (view.GetProxy().TryGetValue(out _view))
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
        public abstract Vector3 MoveDirection();
        /// <summary>
        /// Optional override to setup the FSM
        /// </summary>
        protected virtual void InitStateMachine() { }
        
        public void OnDrawGizmos()
        {
            if (_model != null) _model.OnDraw(transform);
        }

        public void OnDrawGizmosSelected()
        {
            if (_model != null) _model.OnDrawSelected(transform);
        }

        /// <summary>
        /// Cleans up model, view, FSM and all coroutines.
        /// </summary>
        public virtual void Dispose()
        {
            if (_model != null) _model.Dispose();
            _model = null;
            
            if (_view != null) _view.Dispose();
            _view = null;
            
            if (_fsm != null) _fsm.Dispose();
            _fsm = null;
            
            StopAllCoroutines();
            
            OnDispose();
        }

        public void OnDestroy()
        {
            Dispose();
        }
        
        /// <summary>
        /// Hook for subclass specific disposal
        /// </summary>
        protected virtual void OnDispose() { }
    }
}
