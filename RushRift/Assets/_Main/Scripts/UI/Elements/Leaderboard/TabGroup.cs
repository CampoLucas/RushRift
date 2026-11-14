using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TabGroup : MonoBehaviour
{
    [SerializeField] private List<Tab> tabs;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Color disableColor;
    [SerializeField] private int defaultIndex = 0;

    public void Init()
    {
        if (tabs.Count == 0) return;  
        
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            tabs[i].TabButton.onClick.AddListener(() => ShowTab(index));
        }

        ShowTab(defaultIndex);
    }

    public void ShowTab(int index)
    {
        if (tabs.Count == 0) return;  
        for (int i = 0; i < tabs.Count; i++)
        {
            if(i == index)
            {
                tabs[i].TabContent.SetActive(true);
                tabs[i].SelectedButtonImage.color = highlightColor;
            }
            else
            {
                tabs[i].TabContent.SetActive(false);
                tabs[i].SelectedButtonImage.color = disableColor;

            }
        }
    }
}

