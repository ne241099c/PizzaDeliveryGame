using TMPro;
using UnityEngine;

/// <summary>A courier on the team (hub, staff tab). Layout: Assets/Prefabs/UI/Cards/CourierCard.prefab (ScreenPrefabs).</summary>
public sealed class CourierCardView : MonoBehaviour
{
    [SerializeField] private StaffCardHeader header;
    [SerializeField] private AbilityRowParts[] abilities=new AbilityRowParts[5];
    [SerializeField] private RectTransform capacitySlots;
    [SerializeField] private TextMeshProUGUI capacityValue;
    [SerializeField] private RectTransform trainingBox,experienceRow;
    [SerializeField] private TextMeshProUGUI trainingTitle,trainingDetail,experienceName,experienceNext,wage;
    [SerializeField] private UiBar experienceBar;
    [SerializeField] private StickerButton train,cancelTraining,fire;
    public StickerButton Train=>train;
    public StickerButton CancelTraining=>cancelTraining;
    public StickerButton Fire=>fire;

    public void ShowHeader(string name,Color color,(string,Color,Color) role,(string,Color,Color) status)=>header.Show(name,color,role,status);
    public void ShowAbility(int index,string name,float value,float potential)=>abilities[index].Show(name,value,potential);
    /// <summary>Filled slots for the current load, dashed ones up to the potential.</summary>
    public void ShowCapacity(int capacity,int potential)
    {
        var T=UiKit.T;
        for(int i=capacitySlots.childCount-1;i>=0;i--){var old=capacitySlots.GetChild(i);old.SetParent(null,false);Destroy(old.gameObject);}
        for(int s=0;s<Mathf.Max(capacity,potential);s++)
        {
            var slot=UiKit.Box(capacitySlots,"Slot"+s,s*52,0,44,26);
            if(s<capacity){UiKit.Fill(UiKit.Stretch(slot,"Edge"),T.line,8);UiKit.Fill(UiKit.Stretch(slot,"Face",2,2,2,2),T.barFill,6);}
            else UiKit.Dashed(slot,T.inkSub,6,4,2);
        }
        capacityValue.text=capacity.ToString();
    }
    /// <summary>Booked training replaces the experience row, and swaps 訓練 for 訓練をやめる (firing waits).</summary>
    public void ShowTraining(string title,string detail)
    {
        bool booked=title!=null;
        trainingBox.gameObject.SetActive(booked);experienceRow.gameObject.SetActive(!booked);
        if(booked){trainingTitle.text=title;trainingDetail.text=detail;}
        train.gameObject.SetActive(!booked);cancelTraining.gameObject.SetActive(booked);
    }
    public void ShowExperience(string name,float fraction,string next){experienceName.text=name;experienceBar.Set(fraction);experienceNext.text=next;}
    public void ShowWage(string text)=>wage.text=text;
}
