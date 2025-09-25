using Game.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraData : MonoBehaviour
{
    public JointsContainer JointsContainer => jointsContainer;
    public Animator ArmsAnimator => armsAnimator;
    
    [FormerlySerializedAs("joints")] [SerializeField] private JointsContainer jointsContainer;
    [SerializeField] private Animator armsAnimator;

}
