using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Game;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Utils;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSpawner : SingletonBehaviour<PlayerSpawner>
{
    public static NullCheck<PlayerController> Player => _instance.TryGet(out var spawner) ? spawner._player : default;

    #region Spawner Events

    /// <summary>
    /// Called when the player respawns.
    /// Position, position difference and rotation
    /// </summary>
    public static readonly Subject<Vector3, Vector3, Quaternion> PlayerSpawned = new();
    /// <summary>
    /// For when the player reference in the spawner is set.
    /// </summary>
    public static readonly Subject<PlayerController> PlayerSet = new();
    /// <summary>
    /// For when it doesn't have a already made reference or finds a player in the scene.
    /// </summary>
    public static readonly Subject<PlayerController> PlayerCreated = new();
    /// <summary>
    /// For when the player it didn't had a player reference before hand and finds one in the scene.
    /// </summary>
    public static readonly Subject<PlayerController> PlayerFound = new();

    #endregion
    
    [Header("Reference")]
    [SerializeField] private PlayerController instantiatedRef;
    [SerializeField] private PlayerController prefab;

    [Header("Respawn")]
    [SerializeField] private Transform spawn;

    //private NullCheck<Transform> _camera;
    private NullCheck<PlayerController> _player;
    private NullCheck<Rigidbody> _body;
    private CancellationTokenSource _cts;
    
    protected override void OnAwake()
    {
        base.OnAwake();
        _cts = new CancellationTokenSource();

        if (!_player.TryGet(out var player, SetPlayer)) return;
        
        PlayerSet.NotifyAll(player);
        
        if (player.TryGetComponent<Rigidbody>(out var body)) _body = body;
    }

    private PlayerController SetPlayer()
    {
        return instantiatedRef ? instantiatedRef : FindPlayer();
    }

    private PlayerController FindPlayer()
    {
        var existing = FindObjectOfType<PlayerController>();
        if (existing)
        {
            PlayerFound.NotifyAll(existing);
            return existing;
        }

        return CreatePlayer();
    }
    
    private PlayerController CreatePlayer()
    {
        if (!prefab)
        {
            this.Log("Doesn't have a prefab for the player", LogType.Error);
            return null;
        }
        
        var player = Instantiate(prefab);
        PlayerCreated.NotifyAll(player);
        return player;
    }

    public async UniTask Respawn(Vector3 position, Quaternion rotation)
    {
        if (!_player.TryGet(out var player))
        {
            this.Log("Trying to respawn the player when it doesn't exist.", LogType.Error);
            return;
        }
        
        var tr = player.transform;
        if (_body.TryGet(out var body))
            await RigidbodyRespawn(position, rotation, body, tr, _cts.Token);
        else
            await TransformRespawnAsync(position, rotation, tr, _cts.Token);
        
        // Wait for transform sync after physics update
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, _cts.Token);
            
        var diff = tr.position - position;
        PlayerSpawned.NotifyAll(position, diff, rotation);
    }

    private async UniTask RigidbodyRespawn(Vector3 position, Quaternion rotation, Rigidbody body, Transform tr, CancellationToken token)
    {
        // save the previous kinematic state
        var prevKinematic = body.isKinematic;

        // Set velocity to 0
        if (body.isKinematic)
        {
            body.isKinematic = false;
            body.velocity = Vector3.zero;
        }
        
        // Set back as kinematic to reposition.
        body.isKinematic = true;
        
        // Move the player
#if false
        tr.position = position;
        tr.rotation = rotation;
#else
        tr.SetPositionAndRotation(position, rotation);
#endif
        await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate, token);
        
        // Set velocity to 0
        body.isKinematic = false;
        body.velocity = Vector3.zero;
                
        // Set the kinematic to starting state
        body.isKinematic = prevKinematic;
    }

    private async UniTask TransformRespawnAsync(Vector3 position, Quaternion rotation, Transform tr, CancellationToken token)
    {
        tr.SetPositionAndRotation(position, rotation);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
    }

    public async UniTask Respawn(Transform tr) => await Respawn(tr.position, tr.rotation);

    public async UniTask<bool> Respawn(NullCheck<Transform> trCheck)
    {
        if (!trCheck.TryGet(out var tr))
        {
            return false;
        }

        await Respawn(tr);
        return true;
    }

    public static async UniTask RespawnPlayerAsync()
    {
        var playerSpawner = await GetAsync();
        await UniTask.WaitUntil(() => _instance.Get().spawn != null);

        if (playerSpawner.TryGet(out var spawner) && spawner._player.TryGet(out var player))
        {
            await spawner.Respawn(spawner.spawn);
        }
    }

    protected override bool CreateIfNull() => false;
    protected override bool DontDestroy() => false;

    public void SetSpawn(Transform tr)
    {
        spawn = tr;
    }

    protected override void OnDisposeNotInstance()
    {
        base.OnDisposeNotInstance();
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        instantiatedRef = null;
        prefab = null;
        spawn = null;
        _player = null;
        _body = null;
    }

    protected override void OnDisposeInstance()
    {
        base.OnDisposeInstance();
        PlayerSpawned.DetachAll();
        PlayerSet.DetachAll();
        PlayerCreated.DetachAll();
        PlayerFound.DetachAll();
    }
}
