using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RuntimeSuspectManager))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("嫌疑人数据库配置")]
    public List<SuspectScriptableObject> suspectDatabase;

    public RuntimeSuspectManager SuspectManager { get; private set; }

    private void Awake()
    {
        // 1. 单例模式设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 

        // 2. 获取组件引用
        SuspectManager = GetComponent<RuntimeSuspectManager>();
        
        // 3. 【修复位置】将数据初始化提前到 Awake 中
        // 这样可以确保任何其他脚本在 Start() 中请求数据时，字典已经准备完毕
        SuspectManager.Initialize(suspectDatabase);
    }
    
}