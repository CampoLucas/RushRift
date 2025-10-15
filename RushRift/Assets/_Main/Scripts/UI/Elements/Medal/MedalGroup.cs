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
        
        bronze.Init(model.BronzeInfo.MedalTime, model.BronzeInfo.Unlocked);
        silver.Init(model.SilverInfo.MedalTime, model.SilverInfo.Unlocked);
        gold.Init(model.GoldInfo.MedalTime, model.GoldInfo.Unlocked);
    }
}
