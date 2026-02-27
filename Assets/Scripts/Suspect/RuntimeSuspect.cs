using System;
using UnityEngine;

[Serializable]
public class RuntimeSuspect
{
    // 对静态基础数据的引用
    public SuspectScriptableObject BaseData { get; private set; }

    // 下面是运行时状态（可用于存档/读档）
    public bool isDiscovered;        // 是否已被玩家发现
    public bool isInterrogated;      // 是否已进行过审问
    public string currentInkState;   // 用于保存该嫌疑人Ink对话的进度状态(JSON字符串)

    public RuntimeSuspect(SuspectScriptableObject data)
    {
        BaseData = data;
        isDiscovered = false;
        isInterrogated = false;
        currentInkState = "";
    }
}