using System.Collections.Generic;
using UnityEngine;

public class RuntimeSuspectManager : MonoBehaviour
{
    // 字典：通过嫌疑人的ID快速查找其运行时数据
    private Dictionary<string, RuntimeSuspect> suspectDictionary = new Dictionary<string, RuntimeSuspect>();
    
    // 新增：获取所有嫌疑人的运行时数据字典，供 AnalysisManager 等系统遍历使用
    public Dictionary<string, RuntimeSuspect> GetAllSuspects()
    {
        return suspectDictionary;
    }

    // 初始化管理器，由 GameManager 调用
    public void Initialize(List<SuspectScriptableObject> suspectDatas)
    {
        suspectDictionary.Clear();

        foreach (var data in suspectDatas)
        {
            if (data == null) continue;

            if (!suspectDictionary.ContainsKey(data.suspectID))
            {
                RuntimeSuspect newRuntimeSuspect = new RuntimeSuspect(data);
                suspectDictionary.Add(data.suspectID, newRuntimeSuspect);
            }
            else
            {
                Debug.LogWarning($"存在重复的嫌疑人ID: {data.suspectID}");
            }
        }
        Debug.Log($"成功初始化了 {suspectDictionary.Count} 名嫌疑人。");
    }

    // 获取特定嫌疑人的运行时数据
    public RuntimeSuspect GetSuspect(string id)
    {
        if (suspectDictionary.TryGetValue(id, out RuntimeSuspect suspect))
        {
            return suspect;
        }
        Debug.LogError($"找不到ID为 {id} 的嫌疑人！");
        return null;
    }
    
}