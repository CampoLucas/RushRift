using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Elements.LevelSelector
{
    public class Cover : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cover;
        [SerializeField] private GameObject medalSelector;

        [Header("Settings")]
        [SerializeField] private bool startToggled = true;

        private bool _toggled;

        private void Awake()
        {
            Toggle(startToggled);
        }

        public void Toggle(bool value)
        {
            _toggled = value;
            cover.gameObject.SetActive(_toggled);
            medalSelector.gameObject.SetActive(!_toggled);
        }

        public void Toggle()
        {
            Toggle(!_toggled);
        }
        
    }
}