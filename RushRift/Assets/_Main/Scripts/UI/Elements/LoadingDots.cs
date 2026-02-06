using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadingDots : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float interval = .5f;

    private float _timer;
    private int _dots;

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= interval)
        {
            _timer = 0f;
            _dots = (_dots + 1) % 4;
            text.text = new string('.', _dots);
        }
    }
}
