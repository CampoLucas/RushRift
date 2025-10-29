using System;
using System.Collections;
using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Components;
using Game.Utils;
using MyTools.Global;
using TMPro;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
    [SerializeField] private TMP_Text maxSpeed;
    [SerializeField] private TMP_Text currentVelocity;
    
    private PlayerController _target;
    private Rigidbody _rigidbody;

    private void Awake()
    {
#if !UNITY_EDITOR
        Destroy(gameObject);
#endif
        if (!PlayerSpawner.Player.TryGet(out var player))
        {
            this.Log("Player not found", LogType.Error);
            return;
        }
        
        _target = player;
        if (_target && _target.gameObject.TryGetComponent<Rigidbody>(out var rb))
        {
            _rigidbody = rb;
        }
    }


    private void Update()
    {
        // if (_target.GetModel().TryGetComponent<IMovement>(out var movement))
        // {
        //     maxSpeed.text = $"Max Speed: {movement.MaxSpeed}";
        //     currentVelocity.text = $"Current Velocity: {movement.Velocity.magnitude}";
        // }
        
        if (_rigidbody)
        {
            //maxSpeed.text = $"Slippery: {_movement.Slippery:F3}";
            var horVelocity = _rigidbody.velocity.XOZ();
            currentVelocity.text = $"HorVel: {horVelocity.magnitude:F2} || VertVel: {_rigidbody.velocity.y:F2}";
        }
    }
}
