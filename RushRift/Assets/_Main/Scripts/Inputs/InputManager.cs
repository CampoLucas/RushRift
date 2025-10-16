using System;
using System.Collections.Generic;
using Game.UI;
using Game.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Inputs
{
    public class InputManager : MonoBehaviour
    {
        public static HashedKey MoveInput { get; private set; }
        public static HashedKey LookInput { get; private set; }
        public static HashedKey InteractInput { get; private set; }
        public static HashedKey JumpInput { get; private set; }
        
        public static HashedKey PrimaryAttackInput { get; private set; }
        public static HashedKey PrimaryAttackTapInput { get; private set; }
        public static HashedKey PrimaryAttackHoldInput { get; private set; }
        
        public static HashedKey SecondaryAttackInput { get; private set; }
        public static HashedKey PauseInput { get; private set; }
        public static HashedKey MousePosition { get; private set; }
        
        private static InputManager _instance;
        private Dictionary<HashedKey, InputButton> _buttonsDict = new();
        private Dictionary<HashedKey, InputValue<Vector2>> _valuesDict = new();
        private Dictionary<HashedKey, InputAction> _actionsDict = new();

        private PlayerControls _playerControls;

        #region InputFlags

        private bool _heavyFlag;

        #endregion
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            
            _instance = this;
            InitInputs();
        }

        private void Update()
        {
            if (_playerControls.Gameplay.PrimaryAttackHold.triggered)
            {
                _heavyFlag = true;
            }

            if (_playerControls.Gameplay.PrimaryAttackHold.WasReleasedThisFrame())
            {
                _heavyFlag = false;
            }
        }

        private void OnDisable()
        {
            if (_playerControls == null) return;
            _playerControls.Disable();
            CursorHandler.lockState = CursorLockMode.None;
            CursorHandler.visible = true;
        }

        private void OnEnable()
        {
            if (_playerControls == null) _playerControls = new();
            _playerControls.Enable();
        }

        public static bool OnButton(HashedKey key)
        {
            if (_instance == null || !_instance._buttonsDict.TryGetValue(key, out var input)) return false;

            return input.OnHold();
        }

        public static bool OnButtonDown(HashedKey key)
        {
            if (_instance == null || !_instance._buttonsDict.TryGetValue(key, out var input)) return false;

            return input.OnPressed();
        }
        
        public static bool OnButtonUp(HashedKey key)
        {
            if (_instance == null || !_instance._buttonsDict.TryGetValue(key, out var input)) return false;

            return input.OnReleased();
        }

        public static Vector2 GetValueVector(HashedKey key)
        {
            if (_instance == null || !_instance._valuesDict.TryGetValue(key, out var input)) return Vector2.zero;

            return input.GetValue();
        }

        public static bool GetActionPerformed(HashedKey key)
        {
            if (_instance == null || !_instance._actionsDict.TryGetValue(key, out var input)) return false;

            return input.OnPerformed();
        }
        
        public static bool GetActionCanceled(HashedKey key)
        {
            if (_instance == null || !_instance._actionsDict.TryGetValue(key, out var input)) return false;

            return input.OnCanceled();
        }
        
        public static bool GetActionStarted(HashedKey key)
        {
            if (_instance == null || !_instance._actionsDict.TryGetValue(key, out var input)) return false;

            return input.OnStarted();
        }

        private void InitInputs()
        {
            MoveInput = new HashedKey("move");
            LookInput = new HashedKey("look");
            InteractInput = new HashedKey("interact");
            JumpInput = new HashedKey("jump");
            PrimaryAttackInput = new HashedKey("primary");
            PrimaryAttackTapInput = new HashedKey("light");
            PrimaryAttackHoldInput = new HashedKey("heavy");
            SecondaryAttackInput = new HashedKey("secondary");
            PauseInput = new HashedKey("pause");
            MousePosition = new HashedKey("mouse-pos");
            
            AddValueInput(MoveInput, MoveValue);
            AddValueInput(LookInput, LookValue);
            AddValueInput(MousePosition, MousePosValue);
            
            AddActionInput(InteractInput, InteractAction, InteractActionStarted, InteractActionCanceled);
            AddActionInput(JumpInput, JumpAction, JumpActionStarted, JumpActionCanceled);
            AddActionInput(PrimaryAttackTapInput, PrimaryAttackTap, PrimaryAttackTapStarted, PrimaryAttackTapCanceled);
            AddActionInput(PrimaryAttackHoldInput, PrimaryAttackHold, PrimaryAttackHoldStarted, PrimaryAttackHoldCanceled);
            AddActionInput(SecondaryAttackInput, SecondaryAttackAction, SecondaryAttackStarted, SecondaryAttackCanceled);
            AddActionInput(PrimaryAttackInput, PrimaryAttack, PrimaryAttackStarted, PrimaryAttackCanceled);
            
            AddButtonInput(PauseInput, () => _playerControls.UI.Pause.phase == InputActionPhase.Performed, () => _playerControls.UI.Pause.WasPressedThisFrame(), () => _playerControls.UI.Pause.WasReleasedThisFrame());
            AddButtonInput(JumpInput, () => _playerControls.Gameplay.Jump.phase == InputActionPhase.Performed, () => _playerControls.Gameplay.Jump.phase == InputActionPhase.Started, () => _playerControls.Gameplay.Jump.phase == InputActionPhase.Performed);
        }

        #region Add Inputs Methods

        private void AddButtonInput(HashedKey key, Func<bool> onHold, Func<bool> onPressed, Func<bool> onReleased)
        {
            if (_buttonsDict.ContainsKey(key)) return;
            _buttonsDict[key] = new InputButton(onHold, onPressed, onReleased);
        }

        private void AddValueInput(HashedKey key, Func<Vector2> value)
        {
            if (_valuesDict.ContainsKey(key)) return;
            _valuesDict[key] = new InputValue<Vector2>(value);
        }

        private void AddActionInput(HashedKey key, Func<bool> performed, Func<bool> started, Func<bool> canceled)
        {
            if (_actionsDict.ContainsKey(key)) return;
            _actionsDict[key] = new InputAction(performed, started, canceled);
        }
        
        #endregion

        #region Inputs

        private bool InteractAction() => _playerControls.Gameplay.Interact.triggered;
        private bool InteractActionStarted() => _playerControls.Gameplay.Interact.phase == InputActionPhase.Started;
        private bool InteractActionCanceled() => _playerControls.Gameplay.Interact.phase == InputActionPhase.Canceled;
        
        private bool JumpAction() => _playerControls.Gameplay.Jump.triggered;
        private bool JumpActionStarted() => _playerControls.Gameplay.Jump.phase == InputActionPhase.Started;
        private bool JumpActionCanceled() => _playerControls.Gameplay.Jump.phase == InputActionPhase.Canceled;

        private bool PrimaryAttack() => _playerControls.Gameplay.PrimaryAttack.triggered;
        private bool PrimaryAttackStarted() => _playerControls.Gameplay.PrimaryAttack.phase == InputActionPhase.Started;
        private bool PrimaryAttackCanceled() => _playerControls.Gameplay.PrimaryAttack.WasReleasedThisFrame();
        
        private bool PrimaryAttackTap() => _playerControls.Gameplay.PrimaryAttackTap.triggered;
        private bool PrimaryAttackTapStarted() => _playerControls.Gameplay.PrimaryAttackTap.phase == InputActionPhase.Started;
        private bool PrimaryAttackTapCanceled() => _playerControls.Gameplay.PrimaryAttackTap.phase == InputActionPhase.Canceled;
        
        private bool PrimaryAttackHold() => _heavyFlag;
        private bool PrimaryAttackHoldStarted() => _playerControls.Gameplay.PrimaryAttackHold.phase == InputActionPhase.Started;
        private bool PrimaryAttackHoldCanceled() => _playerControls.Gameplay.PrimaryAttackHold.WasReleasedThisFrame();
        
        private bool SecondaryAttackAction() => _playerControls.Gameplay.SecondaryAttack.triggered;
        private bool SecondaryAttackStarted() => _playerControls.Gameplay.SecondaryAttack.phase == InputActionPhase.Started;
        private bool SecondaryAttackCanceled() => _playerControls.Gameplay.SecondaryAttack.phase == InputActionPhase.Canceled;
        
        private Vector2 MoveValue() => _playerControls.Gameplay.Movement.ReadValue<Vector2>();
        private Vector2 LookValue() => _playerControls.Gameplay.Look.ReadValue<Vector2>();
        private Vector2 MousePosValue() => _playerControls.Gameplay.MousePosition.ReadValue<Vector2>();

        #endregion

        private void OnDestroy()
        {
            if (_playerControls != null) _playerControls.Dispose();
            _playerControls = null;
        }
    }

    public readonly struct HashedKey : IEquatable<HashedKey>
    {
        private readonly string _name;
        private readonly int _hashedKey;

        public HashedKey(string name)
        {
            _name = name ?? String.Empty;
            _hashedKey = name.ComputeFNV1aHash();
        }

        public bool Equals(HashedKey other) => _hashedKey == other._hashedKey;
        public override bool Equals(object obj) => obj is HashedKey other && Equals(other);
        public override int GetHashCode() => _hashedKey;
        public override string ToString() => _name;
        public static bool operator ==(HashedKey lhs, HashedKey rhs) => lhs._hashedKey == rhs._hashedKey;
        public static bool operator !=(HashedKey lhs, HashedKey rhs) => lhs._hashedKey != rhs._hashedKey;
    }
    
    public readonly struct InputButton
    {
        private readonly Func<bool> _onHold;
        private readonly Func<bool> _onPressed;
        private readonly Func<bool> _onReleased;

        public InputButton(Func<bool> onHold, Func<bool> onPressed, Func<bool> onReleased)
        {
            _onHold = onHold;
            _onPressed = onPressed;
            _onReleased = onReleased;
        }

        public bool OnHold() => _onHold();
        public bool OnPressed() => _onPressed();
        public bool OnReleased() => _onReleased();
    }

    public readonly struct InputValue<TValue>
    {
        private readonly Func<TValue> _value;

        public InputValue(Func<TValue> value)
        {
            _value = value;
        }

        public TValue GetValue() => _value();
    }

    public readonly struct InputAction
    {
        private readonly Func<bool> _onPerformed;
        private readonly Func<bool> _onStarted;
        private readonly Func<bool> _onCanceled;

        public InputAction(Func<bool> performed, Func<bool> started, Func<bool> canceled)
        {
            _onPerformed = performed;
            _onStarted = started;
            _onCanceled = canceled;
        }

        public bool OnPerformed() => _onPerformed();
        public bool OnStarted() => _onStarted();
        public bool OnCanceled() => _onCanceled();
    }
}
