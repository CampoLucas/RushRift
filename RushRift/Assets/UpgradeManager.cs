using UnityEngine;
using Game.Entities;

[DisallowMultipleComponent]
public class UpgradeManager : MonoBehaviour
{
    [Header("Player Binding")]
    [SerializeField, Tooltip("If assigned, upgrades that target the player will be applied on this controller.")]
    private EntityController playerController;

    [SerializeField, Tooltip("If true and Player is not assigned, tries to find a PlayerController by tag 'Player' on Start.")]
    private bool autoFindPlayerByTag = true;

    [Header("Startup Behavior")]
    [SerializeField, Tooltip("If true, already acquired upgrades for the current level are applied on Start.")]
    private bool applyOwnedUpgradesOnStart = true;
    
    [Header("Debug")]
    [SerializeField, Tooltip("Enable debug logs.")]
    private bool isDebugLoggingEnabled = false;

    private static UpgradeManager _instance;
    
    public static bool HasAppliedOwnedUpgradesThisScene { get; private set; }

    private void Awake()
    {
        HasAppliedOwnedUpgradesThisScene = false;
        _instance = this;
        if (!playerController && autoFindPlayerByTag)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) playerController = go.GetComponentInParent<EntityController>();
        }
    }

    private void Start()
    {
        if (!applyOwnedUpgradesOnStart)
        {
            HasAppliedOwnedUpgradesThisScene = true; // so PlayerController won't try either
            return;
        }

        var data = SaveAndLoad.Load();
        var level = Game.LevelManager.GetResolvedLevelNumber();
        if (level > 0 && data != null)
        {
            var dict = data.LevelsMedalsTimes;
            if (dict.TryGetValue(level, out var mt))
            {
                TryApplyIfAcquired(mt.bronze.upgrade, mt.bronze.isAcquired);
                TryApplyIfAcquired(mt.silver.upgrade, mt.silver.isAcquired);
                TryApplyIfAcquired(mt.gold.upgrade,   mt.gold.isAcquired);
            }
        }

        HasAppliedOwnedUpgradesThisScene = true;
    }

    public static void ProcessLevelCompletion(float runTimeSeconds)
    {
        if (_instance == null) _instance = FindObjectOfType<UpgradeManager>(true);

        if (!Game.LevelManager.TryGetActiveMedal(out var medal) || medal == null) return;

        float bronzeTime = Mathf.Max(0f, medal.levelMedalTimes.bronze.time);
        float silverTime = Mathf.Max(0f, medal.levelMedalTimes.silver.time);
        float goldTime   = Mathf.Max(0f, medal.levelMedalTimes.gold.time);

        bool bronzeNow = bronzeTime > 0f && runTimeSeconds <= bronzeTime && !medal.levelMedalTimes.bronze.isAcquired;
        bool silverNow = silverTime > 0f && runTimeSeconds <= silverTime && !medal.levelMedalTimes.silver.isAcquired;
        bool goldNow   = goldTime   > 0f && runTimeSeconds <= goldTime   && !medal.levelMedalTimes.gold.isAcquired;

        var data = SaveAndLoad.Load();
        if (data == null) return;

        int level = Game.LevelManager.GetResolvedLevelNumber();
        var dict = data.LevelsMedalsTimes; // mutate via getter; do not assign property

        var mtAsset = medal.levelMedalTimes; // value copy
        if (dict.TryGetValue(level, out var mtSaved))
        {
            // keep saved structure in sync with asset (times, upgrade enums/text)
            mtSaved.bronze.time        = mtAsset.bronze.time;        mtSaved.bronze.upgrade        = mtAsset.bronze.upgrade;        mtSaved.bronze.upgradeText = mtAsset.bronze.upgradeText;
            mtSaved.silver.time        = mtAsset.silver.time;        mtSaved.silver.upgrade        = mtAsset.silver.upgrade;        mtSaved.silver.upgradeText = mtAsset.silver.upgradeText;
            mtSaved.gold.time          = mtAsset.gold.time;          mtSaved.gold.upgrade          = mtAsset.gold.upgrade;          mtSaved.gold.upgradeText   = mtAsset.gold.upgradeText;

            if (bronzeNow) mtSaved.bronze.isAcquired = true;
            if (silverNow) mtSaved.silver.isAcquired = true;
            if (goldNow)   mtSaved.gold.isAcquired   = true;

            dict[level] = mtSaved; // mutate dictionary entry
        }
        else
        {
            if (bronzeNow) mtAsset.bronze.isAcquired = true;
            if (silverNow) mtAsset.silver.isAcquired = true;
            if (goldNow)   mtAsset.gold.isAcquired   = true;

            dict[level] = mtAsset; // add new entry
        }

        SaveAndLoad.Save(data);

        if (_instance) _instance.ApplyNewlyEarned(bronzeNow, silverNow, goldNow, medal.levelMedalTimes);
    }

    private void ApplyNewlyEarned(bool bronzeNow, bool silverNow, bool goldNow, medalTimes mt)
    {
        if (bronzeNow) ApplyUpgrade(mt.bronze.upgrade);
        if (silverNow) ApplyUpgrade(mt.silver.upgrade);
        if (goldNow)   ApplyUpgrade(mt.gold.upgrade);
    }

    private void TryApplyIfAcquired(UpgradeEnum upg, bool acquired)
    {
        if (!acquired) return;
        ApplyUpgrade(upg);
    }

    private void ApplyUpgrade(UpgradeEnum upgrade)
    {
        var effect = Game.LevelManager.GetEffect(upgrade);
        if (!effect)
        {
            if (isDebugLoggingEnabled) Debug.LogWarning($"[UpgradeManager] Effect not found for upgrade '{upgrade}'");
            return;
        }

        if (isDebugLoggingEnabled) Debug.Log($"[UpgradeManager] Applying upgrade '{upgrade}' (global-first)", this);

        effect.ApplyEffect(null);

        if (isDebugLoggingEnabled)
            Debug.Log($"[UpgradeManager] After global apply '{upgrade}' → CanUseTerminal={Game.LevelManager.CanUseTerminal}");

        if (playerController)
        {
            effect.ApplyEffect(playerController);

            if (isDebugLoggingEnabled)
                Debug.Log($"[UpgradeManager] After player apply '{upgrade}' → CanUseTerminal={Game.LevelManager.CanUseTerminal}");
        }
    }
}