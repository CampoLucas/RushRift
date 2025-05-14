using System;
using System.Collections;
using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Components;
using TMPro;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
    [SerializeField] private TMP_Text maxSpeed;
    [SerializeField] private TMP_Text currentVelocity;
    
    private IController _target;

    private void Awake()
    {
#if !UNITY_EDITOR
        Destroy(gameObject);
#endif
        _target = FindObjectOfType<PlayerController>();
    }


    private void Update()
    {
        if (_target.GetModel().TryGetComponent<IMovement>(out var movement))
        {
            maxSpeed.text = $"Max Speed: {movement.MaxSpeed}";
            currentVelocity.text = $"Current Velocity: {movement.Velocity.magnitude}";
        }
    }
}
