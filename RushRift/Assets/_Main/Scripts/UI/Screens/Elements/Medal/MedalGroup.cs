using System.Collections;
using System.Collections.Generic;
using Game.UI.Screens;
using UnityEngine;

public class MedalGroup : MonoBehaviour
{
    [SerializeField] private LevelWonPresenter presenter;
    [SerializeField] private Medal bronze;
    [SerializeField] private Medal silver;
    [SerializeField] private Medal gold;

    public void Init()
    {
        var model = presenter.GetModel();
        
        bronze.Init(model.BronzeInfo.MedalTime);
        silver.Init(model.SilverInfo.MedalTime);
        gold.Init(model.GoldInfo.MedalTime);
    }
}
