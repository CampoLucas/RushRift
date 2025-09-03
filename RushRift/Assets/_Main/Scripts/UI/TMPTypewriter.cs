using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[DisallowMultipleComponent]
public class TMPTypewriter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("TextMeshPro target. If not set, the component on this GameObject will be used.")]
    private TMP_Text targetText;

    [SerializeField, Tooltip("Text to type for Auto Start or Collider Activation. If empty, uses current text on target.")]
    [TextArea(2, 6)]
    private string initialFullText = "";

    [Header("Auto Start")]
    [SerializeField, Tooltip("Begin typing automatically on OnEnable().")]
    private bool autoStartOnEnable = true;

    [Header("Typing Speed")]
    [SerializeField, Tooltip("Characters per second.")]
    private float charactersPerSecond = 30f;

    [SerializeField, Tooltip("Random +/- seconds added per character to vary rhythm.")]
    private float randomDelayJitterSeconds = 0f;

    [SerializeField, Tooltip("Use unscaled time (ignores Time.timeScale).")]
    private bool useUnscaledTime = false;

    [Header("Per-Character Delay Multipliers")]
    [SerializeField, Tooltip("Delay multiplier applied after punctuation characters (.,!?:;)")]
    private float punctuationDelayMultiplier = 3f;

    [SerializeField, Tooltip("Delay multiplier applied after whitespace (space, tab, newline). Set < 1 to speed up, 0 to skip.")]
    private float whitespaceDelayMultiplier = 0.25f;

    [Header("Events")]
    [SerializeField, Tooltip("Invoked when typing completes.")]
    private UnityEvent onTypingCompleted;

    [Header("Collider Activation")]
    [SerializeField, Tooltip("Optional: a separate GameObject that contains the collider used to activate typing. If null, this GameObject is used.")]
    private GameObject activationColliderObject;

    [SerializeField, Tooltip("If enabled, BeginTyping is called when a qualifying collider ENTERS the trigger.")]
    private bool activateOnTriggerEnter = true;

    [SerializeField, Tooltip("If enabled, BeginTyping is called when a qualifying collider COLLIDES with the object.")]
    private bool activateOnCollisionEnter = false;

    [SerializeField, Tooltip("Objects with this tag can trigger typing. Leave empty to accept any.")]
    private string requiredActivatorTag = "Player";

    [SerializeField, Tooltip("If true, collider activation can happen only once. Further entries are ignored.")]
    private bool triggerOnlyOnce = true;

    [SerializeField, Tooltip("If true and typing is already in progress, a new collider activation restarts typing from the beginning.")]
    private bool restartTypingIfTriggeredWhileInProgress = false;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints debug logs.")]
    private bool isDebugLoggingEnabled = false;

    private Coroutine typingCoroutine;
    private string currentFullText;
    private int totalVisibleCharacters;
    private bool isPaused;
    private bool hasTriggeredOnce;

    // Forwarder we add at runtime if a separate collider object is assigned
    private TMPTypewriterActivatorForwarder createdForwarder;
    private bool weCreatedForwarder;

    public bool IsTyping => typingCoroutine != null;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (!targetText) targetText = GetComponent<TMP_Text>();
        if (!targetText) Debug.LogError("[TMPTypewriter] Missing TMP_Text target.", this);

        EnsureColliderForwarder();
    }

    private void OnEnable()
    {
        if (!targetText) return;

        if (autoStartOnEnable)
        {
            var text = string.IsNullOrEmpty(initialFullText) ? targetText.text : initialFullText;
            BeginTyping(text);
        }
    }

    private void OnDestroy()
    {
        // Clean up the forwarder we created (if any)
        if (weCreatedForwarder && createdForwarder)
        {
            Destroy(createdForwarder);
            createdForwarder = null;
            weCreatedForwarder = false;
        }
    }

    // -------- Collider Activation on THIS object (kept for convenience) --------

    private void OnTriggerEnter(Collider other)
    {
        // Only process if no separate collider object is set (or it IS this object)
        if (activationColliderObject && activationColliderObject != gameObject) return;
        if (!activateOnTriggerEnter) return;
        var go = other ? (other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject) : null;
        RequestActivationFrom(go, "OnTriggerEnter");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (activationColliderObject && activationColliderObject != gameObject) return;
        if (!activateOnCollisionEnter) return;
        var go = collision != null
            ? (collision.rigidbody ? collision.rigidbody.gameObject : collision.gameObject)
            : null;
        RequestActivationFrom(go, "OnCollisionEnter");
    }

    // -------- Public activation request (used by forwarder too) --------

    public void RequestActivationFrom(GameObject activator, string source)
    {
        if (triggerOnlyOnce && hasTriggeredOnce)
        {
            Log($"{source}: ignored (triggerOnlyOnce).");
            return;
        }

        if (!activator)
        {
            Log($"{source}: null activator.");
            return;
        }

        if (!string.IsNullOrEmpty(requiredActivatorTag) && !activator.CompareTag(requiredActivatorTag))
        {
            Log($"{source}: '{activator.name}' ignored (tag={activator.tag}, required={requiredActivatorTag}).");
            return;
        }

        if (IsTyping && !restartTypingIfTriggeredWhileInProgress)
        {
            Log($"{source}: already typing; restart disabled.");
            return;
        }

        var text = string.IsNullOrEmpty(initialFullText) ? targetText.text : initialFullText;
        BeginTyping(text);
        hasTriggeredOnce = true;
        Log($"{source}: BeginTyping triggered by '{activator.name}'.");
    }

    // -------- Forwarder setup for external collider --------

    private void EnsureColliderForwarder()
    {
        if (!activationColliderObject || activationColliderObject == gameObject) return;

        var colliderOnTarget = activationColliderObject.GetComponent<Collider>();
        if (!colliderOnTarget)
        {
            Debug.LogWarning($"[TMPTypewriter] The assigned Activation Collider Object '{activationColliderObject.name}' has no Collider.", this);
            return;
        }

        // Add a forwarder to that object (separate component per owner keeps it simple)
        createdForwarder = activationColliderObject.AddComponent<TMPTypewriterActivatorForwarder>();
        createdForwarder.hideFlags = HideFlags.HideInInspector;
        createdForwarder.Initialize(this, activateOnTriggerEnter, activateOnCollisionEnter);
        weCreatedForwarder = true;

        Log($"Forwarder added to '{activationColliderObject.name}' (trigger={activateOnTriggerEnter}, collision={activateOnCollisionEnter}).");
    }

    // -------- Typing Core --------

    public void BeginTyping(string textToType)
    {
        if (!targetText) return;
        StopTyping();

        currentFullText = textToType ?? string.Empty;
        targetText.text = currentFullText;
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();
        totalVisibleCharacters = targetText.textInfo.characterCount;

        Log($"BeginTyping: totalVisibleCharacters={totalVisibleCharacters}");
        typingCoroutine = StartCoroutine(TypeCharactersCoroutine());
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            Log("StopTyping");
        }
    }

    public void SkipToEnd()
    {
        if (!targetText) return;
        targetText.maxVisibleCharacters = totalVisibleCharacters;
        StopTyping();
        onTypingCompleted?.Invoke();
        Log("SkipToEnd -> full text shown.");
    }

    public void SetPaused(bool pause)
    {
        isPaused = pause;
        Log(pause ? "Paused" : "Resumed");
    }

    private IEnumerator TypeCharactersCoroutine()
    {
        if (totalVisibleCharacters == 0)
        {
            typingCoroutine = null;
            onTypingCompleted?.Invoke();
            yield break;
        }

        int visibleCount = 0;
        float baseDelay = 1f / Mathf.Max(1f, charactersPerSecond);

        while (visibleCount < totalVisibleCharacters)
        {
            while (isPaused) yield return null;

            visibleCount++;
            targetText.maxVisibleCharacters = visibleCount;

            char revealedChar = GetVisibleChar(visibleCount - 1);

            float mult = GetDelayMultiplier(revealedChar);
            float delay = Mathf.Max(
                0f,
                baseDelay * mult + (randomDelayJitterSeconds == 0f ? 0f : Random.Range(-randomDelayJitterSeconds, randomDelayJitterSeconds))
            );

            if (delay > 0f) yield return WaitForSecondsSmart(delay);
            else yield return null;
        }

        typingCoroutine = null;
        onTypingCompleted?.Invoke();
        Log("Typing complete.");
    }

    private IEnumerator WaitForSecondsSmart(float seconds)
    {
        if (useUnscaledTime)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
        }
        else
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }
    }

    private char GetVisibleChar(int visibleIndex)
    {
        if (!targetText || targetText.textInfo.characterCount == 0) return '\0';
        if (visibleIndex < 0 || visibleIndex >= targetText.textInfo.characterCount) return '\0';
        return targetText.textInfo.characterInfo[visibleIndex].character;
    }

    private float GetDelayMultiplier(char c)
    {
        if (char.IsWhiteSpace(c)) return whitespaceDelayMultiplier;
        switch (c)
        {
            case '.': case ',': case '!': case '?': case ':': case ';':
                return punctuationDelayMultiplier;
            default:
                return 1f;
        }
    }

    private void Log(string message)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[TMPTypewriter] {name}: {message}", this);
    }

    // ---------- forwarder component ----------
    [AddComponentMenu("")]
    private sealed class TMPTypewriterActivatorForwarder : MonoBehaviour
    {
        private TMPTypewriter owner;
        private bool forwardTriggerEnter;
        private bool forwardCollisionEnter;

        public void Initialize(TMPTypewriter owner, bool trigger, bool collision)
        {
            this.owner = owner;
            forwardTriggerEnter = trigger;
            forwardCollisionEnter = collision;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!forwardTriggerEnter || owner == null) return;
            var go = other ? (other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject) : null;
            owner.RequestActivationFrom(go, "Forwarder.OnTriggerEnter");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!forwardCollisionEnter || owner == null) return;
            var go = collision != null
                ? (collision.rigidbody ? collision.rigidbody.gameObject : collision.gameObject)
                : null;
            owner.RequestActivationFrom(go, "Forwarder.OnCollisionEnter");
        }
    }
}