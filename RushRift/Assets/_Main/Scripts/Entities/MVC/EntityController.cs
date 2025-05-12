using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Entities
{
    public abstract class EntityController : MonoBehaviour, IController
    {
        public Transform Origin { get; private set; }
        public VisualEffect SpeedLines => speedLines;
        public Joints<EntityJoint> Joints => joints;

        [Header("Data")]
        [SerializeField] protected EntityModelSO model;
        [SerializeField] protected EntityViewSO view;

        [Header("References")]
        [SerializeField] protected Joints<EntityJoint> joints;
        [SerializeField] private Animator[] animator;
        [SerializeField] private VisualEffect speedLines;

        protected EntityStateMachine _fsm;
        
        private IModel _model;
        private IView _view;

        protected virtual void Awake()
        {
            Origin = transform;

            // Create the model
            if (model.GetProxy().TryGetValue(out _model))
            {
                _model.Init(this);
            }

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

        public IModel GetModel() => _model;
        public IView GetView() => _view;
        public abstract Vector3 MoveDirection();
        protected virtual void InitStateMachine() { }

        public Coroutine DoCoroutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void EndCoroutine(Coroutine coroutine)
        {
            StopCoroutine(coroutine);
        }

        public void EndAllCoroutines()
        {
            StopAllCoroutines();
        }

        public void OnDrawGizmos()
        {
            if (_model != null) _model.OnDraw(transform);
        }

        public void OnDrawGizmosSelected()
        {
            if (_model != null) _model.OnDrawSelected(transform);
        }

        public virtual void Dispose()
        {
            if (_model != null) _model.Dispose();
            _model = null;
            
            if (_view != null) _view.Dispose();
            _view = null;
            
            if (_fsm != null) _fsm.Dispose();
            _fsm = null;
            
            StopAllCoroutines();
        }

        public void OnDestroy()
        {
            Dispose();
        }
    }
}
