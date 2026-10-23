using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Roster of kitchen staff. Shares the team limit with couriers (see RecruitmentManager).</summary>
public sealed class KitchenCrew : MonoBehaviour
{
    [SerializeField] private List<KitchenStaff> crew=new(){new KitchenStaff("Mio",.4f,.4f)};
    private List<KitchenStaff> starting;
    public IReadOnlyList<KitchenStaff> People=>crew;
    public int Count=>crew.Count;
    public event Action Changed;
    private void Awake()=>starting=crew.ConvertAll(s=>s.Clone());
    public bool Add(KitchenStaff staff)
    {
        if(staff==null || crew.Exists(s=>s.Name==staff.Name))return false;
        crew.Add(staff);
        Changed?.Invoke();
        return true;
    }
    public bool Remove(KitchenStaff staff){if(!crew.Remove(staff))return false;Changed?.Invoke();return true;}
    public void ResetToInitial(){if(starting==null)Awake();crew=starting.ConvertAll(s=>s.Clone());Changed?.Invoke();}
    public void Restore(IEnumerable<KitchenStaff> restored){crew=new List<KitchenStaff>(restored);Changed?.Invoke();}
    public void ResetDailyState(){foreach(var staff in crew)staff.IsWorking=false;}
}
