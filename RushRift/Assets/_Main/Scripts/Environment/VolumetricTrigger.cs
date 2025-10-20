using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.LevelElements.Terminal;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class VolumetricTrigger : MonoBehaviour, ISubject<string>
{
    public enum ActionType { None, SendOn, SendOff, Toggle }

    [Header("Area")]
    [SerializeField, Tooltip("Collider that defines the trigger volume. If null, uses the Collider on this GameObject.")]
    private Collider areaCollider;
    [SerializeField, Tooltip("If true, sets the collider to IsTrigger on validate.")]
    private bool autoConfigureAsTrigger = true;
    [SerializeField, Tooltip("Layers considered as activators.")]
    private LayerMask activatorLayerMask = ~0;
    [SerializeField, Tooltip("Only objects with this tag are valid. Leave empty to accept any.")]
    private string requiredActivatorTag = "Player";
    [SerializeField, Tooltip("Include trigger colliders when probing overlaps.")]
    private bool includeTriggerColliders = true;

    [Header("Initial State")]
    [SerializeField, Tooltip("If true, sends an action at startup after the optional delay.")]
    private bool applyInitialStateOnStart;
    [SerializeField, Tooltip("Action to send at startup.")]
    private ActionType initialAction = ActionType.SendOff;
    [SerializeField, Tooltip("Delay in seconds before sending the initial action.")]
    private float initialActionDelaySeconds;
    [SerializeField, Tooltip("Internal state flag used by Toggle actions.")]
    private bool startsOn;

    [Header("On Enter")]
    [SerializeField, Tooltip("Action invoked when a valid activator ENTERS the volume.")]
    private ActionType onEnterAction = ActionType.SendOn;
    [SerializeField, Tooltip("Delay in seconds before invoking the On Enter action.")]
    private float onEnterDelaySeconds;

    [Header("On Exit")]
    [SerializeField, Tooltip("Action invoked when a valid activator EXITS the volume.")]
    private ActionType onExitAction = ActionType.None;
    [SerializeField, Tooltip("Delay in seconds before invoking the On Exit action.")]
    private float onExitDelaySeconds;

    [Header("Constraints")]
    [SerializeField, Tooltip("If true, the trigger fires only once for its lifetime.")]
    private bool triggerOnlyOnce;
    [SerializeField, Tooltip("Minimum seconds between consecutive actions.")]
    private float minIntervalBetweenActions;

    [Header("Observers")]
    [SerializeField, Tooltip("Observers that receive ON/OFF notifications.")]
    private ObserverComponent[] observers;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled;
    [SerializeField, Tooltip("Draw gizmos for the trigger volume.")]
    private bool drawGizmos = true;

    private ISubject<string> subject = new Subject<string>();
    private bool state;
    private bool hasFiredOnce;
    private float lastActionTime = -999f;
    private readonly HashSet<GameObject> occupants = new();

    private void Awake()
    {
        if (!areaCollider) areaCollider = GetComponent<Collider>();
        foreach (ObserverComponent o in observers) if (o) subject.Attach(o);
        state = startsOn;
    }

    private void OnValidate()
    {
        if (!areaCollider) areaCollider = GetComponent<Collider>();
        if (autoConfigureAsTrigger && areaCollider) areaCollider.isTrigger = true;
        minIntervalBetweenActions = Mathf.Max(0f, minIntervalBetweenActions);
        onEnterDelaySeconds = Mathf.Max(0f, onEnterDelaySeconds);
        onExitDelaySeconds = Mathf.Max(0f, onExitDelaySeconds);
        initialActionDelaySeconds = Mathf.Max(0f, initialActionDelaySeconds);
    }

    private void Start()
    {
        if (applyInitialStateOnStart && initialAction != ActionType.None)
            StartCoroutine(InvokeActionAfterDelay(initialAction, initialActionDelaySeconds));
    }

    private void OnDestroy()
    {
        observers = null;
        subject.DetachAll();
        subject.Dispose();
        subject = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidActivator(other)) return;
        if (!occupants.Contains(GetRoot(other))) occupants.Add(GetRoot(other));
        if (onEnterAction == ActionType.None) return;
        if (IsRateLimitedOrOneShot()) return;
        StartCoroutine(InvokeActionAfterDelay(onEnterAction, onEnterDelaySeconds));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsValidActivator(other)) return;
        occupants.Remove(GetRoot(other));
        if (onExitAction == ActionType.None) return;
        if (IsRateLimitedOrOneShot()) return;
        StartCoroutine(InvokeActionAfterDelay(onExitAction, onExitDelaySeconds));
    }

    private IEnumerator InvokeActionAfterDelay(ActionType action, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (triggerOnlyOnce && hasFiredOnce) yield break;
        
        DoAction(action);
        hasFiredOnce = triggerOnlyOnce || hasFiredOnce;
        lastActionTime = Time.time;
    }

    private void DoAction(ActionType action)
    {
        switch (action)
        {
            case ActionType.SendOn:
                subject.NotifyAll(Terminal.ON_ARGUMENT);
                state = true;
                Log("Notify ON");
                break;
            
            case ActionType.SendOff:
                subject.NotifyAll(Terminal.OFF_ARGUMENT);
                state = false;
                Log("Notify OFF");
                break;
            
            case ActionType.Toggle:
                string arg = state ? Terminal.OFF_ARGUMENT : Terminal.ON_ARGUMENT;
                subject.NotifyAll(arg);
                state = !state;
                Log($"Notify {arg.ToUpper()} (Toggle)");
                break;
        }
    }

    private bool IsValidActivator(Collider col)
    {
        if (!col) return false;
        GameObject root = GetRoot(col);
        if (!root) return false;
        if (!string.IsNullOrEmpty(requiredActivatorTag) && !root.CompareTag(requiredActivatorTag)) return false;
        if ((activatorLayerMask.value & (1 << root.layer)) == 0) return false;
        if (!includeTriggerColliders && col.isTrigger) return false;
        return true;
    }

    private static GameObject GetRoot(Collider c) =>
        c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;

    public bool Attach(IObserver<string> observer, bool disposeOnDetach = false) => subject.Attach(observer, disposeOnDetach);
    public bool Detach(IObserver<string> observer) => subject.Detach(observer);
    
    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
    
    public void DetachAll() => subject.DetachAll();
    public void NotifyAll(string arg) => subject.NotifyAll(arg);

    private bool IsRateLimitedOrOneShot()
    {
        if (triggerOnlyOnce && hasFiredOnce) return true;
        if (Time.time - lastActionTime < minIntervalBetweenActions) return true;
        return false;
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[VolumetricTrigger] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Collider col = areaCollider ? areaCollider : GetComponent<Collider>();
        if (!col) return;

        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.18f);
        if (col is BoxCollider box)
        {
            Matrix4x4 m = Matrix4x4.TRS(box.transform.TransformPoint(box.center), box.transform.rotation, Vector3.Scale(box.size, box.transform.lossyScale));
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            Bounds b = col.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
#endif
}
