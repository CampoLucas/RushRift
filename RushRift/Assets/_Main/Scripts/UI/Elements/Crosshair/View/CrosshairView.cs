using System;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    [RequireComponent(typeof(Canvas))]
    public abstract class CrosshairView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;

        private void Awake()
        {
            if (!canvas) canvas = GetComponent<Canvas>();
        }

        public abstract void Initialize(IController controller);

        public void Show()
        {
            canvas.enabled = true;
            OnShow();
        }

        public void Hide()
        {
            canvas.enabled = false;
            OnHide();
        }
        
        protected abstract void OnShow();
        protected abstract void OnHide();
    }
}