using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tab : MonoBehaviour
{
    [SerializeField] private Button tabButton;
    [SerializeField] private GameObject tabContent;

    public Button TabButton { get => tabButton; set => tabButton = value; }
    public GameObject TabContent { get => tabContent; set => tabContent = value; }
}
