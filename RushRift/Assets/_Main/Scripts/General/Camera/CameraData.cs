using System.Collections;
using System.Collections.Generic;
using Game.Entities;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraData : MonoBehaviour
{
    public JointsContainer JointsContainer => jointsContainer;
    public Animator ArmsAnimator => armsAnimator;
    
    [FormerlySerializedAs("joints")] [SerializeField] private JointsContainer jointsContainer;
    [SerializeField] private Animator armsAnimator;

}
