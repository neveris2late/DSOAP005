using System.Collections.Generic;
using UnityEngine;

// 强制挂载 GameManager 时自动添加 RuntimeSuspectManager
[RequireComponent(typeof(RuntimeSuspectManager))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("嫌疑人数据库配置")]
    [Tooltip("在这里拖入所有创建好的 SuspectScriptableObject 资产")]
    public List<SuspectScriptableObject> suspectDatabase;

    // 对外暴露运行时管理器的引用
    public RuntimeSuspectManager SuspectManager { get; private set; }

    private void Awake()
    {
        // 单例模式设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 保证切场景时不被销毁

        // 获取组件引用
        SuspectManager = GetComponent<RuntimeSuspectManager>();
    }

    private void Start()
    {
        // 游戏开始时，将 Inspector 中拖入的数据传递给 RuntimeManager 进行初始化
        SuspectManager.Initialize(suspectDatabase);
    }
}