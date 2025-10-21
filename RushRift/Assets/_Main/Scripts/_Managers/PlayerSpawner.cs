using System;
using System.Collections;
using System.Collections.Generic;
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
    public static Subject<Vector3, Vector3, Quaternion> PlayerSpawned = new();
    
    /// <summary>
    /// For when the player reference in the spawner is set.
    /// </summary>
    public static Subject<PlayerController> PlayerSet = new();
    /// <summary>
    /// For when it doesn't have a already made reference or finds a player in the scene.
    /// </summary>
    public static Subject<PlayerController> PlayerCreated = new();
    /// <summary>
    /// For when the player it didn't had a player reference before hand and finds one in the scene.
    /// </summary>
    public static Subject<PlayerController> PlayerFound = new();
    
    [Header("Reference")]
    [SerializeField] private PlayerController instantiatedRef;
    [SerializeField] private PlayerController prefab;

    [Header("Respawn")]
    [SerializeField] private Transform spawn;
    [SerializeField] private float minDistance = 0.1f;

    //private NullCheck<Transform> _camera;
    private NullCheck<PlayerController> _player;
    private NullCheck<Rigidbody> _body;
    protected override void OnAwake()
    {
        base.OnAwake();

        if (!_player.TryGet(out var player, SetPlayer))
        {
            return;
        }
        PlayerSet.NotifyAll(player);
        
        if (!player.TryGetComponent<Rigidbody>(out var body))
        {
            return;
        }
        _body = body;
    }

    private PlayerController SetPlayer()
    {
        return instantiatedRef ? instantiatedRef : FindPlayer();
    }

    private PlayerController FindPlayer()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player)
        {
            PlayerFound.NotifyAll(player);
        }

        return CreatePlayer();
    }
    
    private PlayerController CreatePlayer()
    {
        var player = Instantiate(prefab);
        PlayerCreated.NotifyAll(player);
        return player;
    }

    private void Update()
    {
        if (spawn && Input.GetKeyDown(KeyCode.R))
        {
            Respawn(spawn);
        }
    }

    public async UniTask Respawn(Vector3 position, Quaternion rotation)
    {
        Debug.Log("[SuperTest] Respawn player");
        if (!_player.TryGet(out var player))
        {
            this.Log("Trying to respawn the player when it doesn't exist.", LogType.Error);
            return;
        }
        
        var tr = player.transform;
        if (_body.TryGet(out var body))
        {
            await RigidbodyRespawn(position, rotation, body, tr, minDistance);
        }
        else
        {
            await TransformRespawnAsync(position, rotation, tr, minDistance);
        }
        
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            
        var diff = tr.position - position;
        PlayerSpawned.NotifyAll(position, diff, rotation);
    }

    private async UniTask RigidbodyRespawn(Vector3 position, Quaternion rotation, Rigidbody body, Transform tr, float minDist = .1f)
    {
        // save the previous kinematic state
        var prevKinematic = body.isKinematic;

        // Set velocity to 0
        if (body.isKinematic)
        {
            body.isKinematic = false;
            body.velocity = Vector3.zero;
            // Set as kinematic
            body.isKinematic = true;
        }
        
        // Move the player
        tr.position = position;
        tr.rotation = rotation;
        // tr.position = position;
        // tr.rotation = rotation;

        await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
        
        // Set velocity to 0
        body.isKinematic = false;
        body.velocity = Vector3.zero;
                
        // Set the kinematic to starting state
        body.isKinematic = prevKinematic;
    }

    private async UniTask TransformRespawnAsync(Vector3 position, Quaternion rotation, Transform tr, float minDist = .1f)
    {
        tr.SetPositionAndRotation(position, rotation);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        await UniTask.WaitUntil(() => 
            tr.IsNullOrMissingReference() || // Use cancelation tokes instead of this
            Vector3.Distance(tr.position, position) <= minDist);
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
}
