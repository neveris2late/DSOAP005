using System;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeSuspectManager : MonoBehaviour
{
    private Dictionary<string, RuntimeSuspect> suspectDictionary = new Dictionary<string, RuntimeSuspect>();
    
    // 当前被选中的嫌疑人
    public RuntimeSuspect CurrentSuspect { get; private set; }
    
    // 当选中嫌疑人改变时触发的事件
    public event Action<RuntimeSuspect> OnCurrentSuspectChanged;

    // 存储今日登场的嫌疑人列表
    public List<RuntimeSuspect> DailyActiveSuspects { get; private set; } = new List<RuntimeSuspect>();

    public Dictionary<string, RuntimeSuspect> GetAllSuspects()
    {
        return suspectDictionary; //
    }

    public void Initialize(List<SuspectScriptableObject> suspectDatas)
    {
        suspectDictionary.Clear();
        CurrentSuspect = null; 

        foreach (var data in suspectDatas)
        {
            if (data == null) continue;

            if (!suspectDictionary.ContainsKey(data.suspectID))
            {
                RuntimeSuspect newRuntimeSuspect = new RuntimeSuspect(data);
                suspectDictionary.Add(data.suspectID, newRuntimeSuspect);
            }
        }
    }

    public RuntimeSuspect GetSuspect(string id)
    {
        if (suspectDictionary.TryGetValue(id, out RuntimeSuspect suspect))
        {
            return suspect;
        }
        return null; //
    }

    // 设置今日登场的 4 个嫌疑人（在进入分析场景前或 Start 时调用）
    public void SetupDailyLineup(List<string> dailySuspectIDs)
    {
        DailyActiveSuspects.Clear();
        foreach (string id in dailySuspectIDs)
        {
            RuntimeSuspect suspect = GetSuspect(id);
            if (suspect != null) DailyActiveSuspects.Add(suspect);
        }
        
        // 默认选中第一个
        if (DailyActiveSuspects.Count > 0)
        {
            SetCurrentSuspect(DailyActiveSuspects[0].BaseData.suspectID);
        }
    }

    // 设置当前正在查看/分析的嫌疑人
    public void SetCurrentSuspect(string id)
    {
        RuntimeSuspect target = GetSuspect(id);
        if (target != null)
        {
            CurrentSuspect = target;
            OnCurrentSuspectChanged?.Invoke(CurrentSuspect);
        }
    }
}