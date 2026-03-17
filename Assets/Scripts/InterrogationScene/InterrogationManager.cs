using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;

public class InterrogationManager : MonoBehaviour
{
    private RuntimeSuspectManager suspectManager;
    
    [Header("场景与UI引用")]
    public Transform sceneContainer; 
    public InterrogationSceneUiManager uiManager;

    private GameObject currentSceneInstance;
    
    private int currentTestIndex = 0; // 用于测试顺序切换

    void Start()
    {
        suspectManager = GameManager.Instance.SuspectManager;

        if (suspectManager.DailyActiveSuspects.Count == 0)
        {
            CreateTestLineup();
        }

        suspectManager.OnCurrentSuspectChanged += HandleSuspectDataDistributed;

        if (suspectManager.CurrentSuspect != null)
        {
            HandleSuspectDataDistributed(suspectManager.CurrentSuspect);
        }
    }

    void OnDestroy()
    {
        if (suspectManager != null)
        {
            suspectManager.OnCurrentSuspectChanged -= HandleSuspectDataDistributed;
        }
    }

    void Update()
    {
        // 测试按键
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSuspectIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSuspectIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSuspectIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSuspectIndex(3);
        
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSuspectIndex(0);
        // ...

        // 按9手动加载下一位嫌疑人并结算
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ProcessNextSuspectTransition();
        }
    }
    
    private void ProcessNextSuspectTransition()
    {
        var dailySuspects = suspectManager.DailyActiveSuspects;
        if (dailySuspects.Count == 0) return;

        RuntimeSuspect previousSuspect = suspectManager.CurrentSuspect;
        
        // 1. 评估上一位嫌疑人的判定结果
        if (previousSuspect != null && previousSuspect.finalJudgment != PlayerJudgment.None)
        {
            string resultMessage = EvaluateJudgment(previousSuspect);
            
            // 【修改点】：调用 UIManager 播放UI演出
            if (uiManager != null)
            {
                uiManager.ShowResultPrompt(resultMessage);
            }
        }

        // 2. 切换到下一位
        currentTestIndex++;
        if (currentTestIndex >= dailySuspects.Count)
        {
            currentTestIndex = 0; // 循环，或者处理今日审讯结束的逻辑
            Debug.Log("今日嫌疑人已全部审问完毕！");
        }
        
        SwitchToSuspectIndex(currentTestIndex);
    }
    
    /// <summary>
    /// 判定身份函数
    /// </summary>
    /// <param name="suspect"></param>
    /// <returns></returns>
    private string EvaluateJudgment(RuntimeSuspect suspect)
    {
        bool isSpy = suspect.BaseData.isSpy;
        bool isAndroid = suspect.BaseData.isAndroid;
        PlayerJudgment judgment = suspect.finalJudgment;
        string name = suspect.BaseData.suspectName;

        // 逻辑优先级：如果同时是仿生人和间谍，必须按仿生人处理才算彻底解决威胁
        if (isAndroid)
        {
            if (judgment == PlayerJudgment.TestAndroid) 
                return "该嫌疑人已被脊髓测试认定为仿生人。";
            if (judgment == PlayerJudgment.ExecuteAndroid) 
                return "该嫌疑人尸体已被警署回收，确认为仿生人。";
            
            return $"调查员发现嫌疑人 {name} 已经逃离了川流城，您的错误判断，让重要的嫌疑人脱逃，警署将对该失职行为展开调查。";
        }
        else if (isSpy)
        {
            if (judgment == PlayerJudgment.ArrestSpy) 
                return $"警署已对嫌疑人 {name} 进行了进一步审讯，锁定了更多证据。";
            
            return $"调查员发现嫌疑人 {name} 已经逃离了川流城，您的错误判断，让重要的嫌疑人脱逃，警署将对该失职行为展开调查。";
        }
        else // 普通人
        {
            if (judgment == PlayerJudgment.Innocent) 
                return ""; // 普通人判定正确，不提示
            
            string action = judgment == PlayerJudgment.ArrestSpy ? "逮捕" : (judgment == PlayerJudgment.ExecuteAndroid ? "击杀" : "测试");
            return $"您错误{action}了嫌疑人 {name}，警署已对您的错误行为进行了登记。";
        }
    }

    private void SwitchToSuspectIndex(int index)
    {
        var dailySuspects = suspectManager.DailyActiveSuspects;
        if (index >= 0 && index < dailySuspects.Count)
        {
            string targetID = dailySuspects[index].BaseData.suspectID;
            suspectManager.SetCurrentSuspect(targetID);
        }
    }

    private void HandleSuspectDataDistributed(RuntimeSuspect suspectData)
    {
        if (currentSceneInstance != null) Destroy(currentSceneInstance);

        // ==========================================
        // 【新增】切换专属 BGM 和 环境音 (Ambient)
        // ==========================================
        if (AudioManager.Instance != null)
        {
            // 1. 播放或停止 BGM
            if (suspectData.BaseData.bgmClip != null)
            {
                // 使用默认的 1 秒淡入淡出切换
                AudioManager.Instance.PlayBGM(suspectData.BaseData.bgmClip);
            }
            else
            {
                // 如果当前嫌疑人没有配置 BGM，则让音乐淡出停止
                AudioManager.Instance.StopBGM();
            }

            // 2. 播放或停止 环境音
            if (suspectData.BaseData.ambientClip != null)
            {
                AudioManager.Instance.PlayAmbient(suspectData.BaseData.ambientClip);
            }
            else
            {
                AudioManager.Instance.StopAmbient();
            }
        }

        if (suspectData.BaseData.interrogationScenePrefab != null)
        {
            // 1. 实例化 Prefab
            currentSceneInstance = Instantiate(suspectData.BaseData.interrogationScenePrefab, sceneContainer);

            // === 场景实例入场动画 ===
            
            // 2. 设置初始缩放为 1.2 倍
            currentSceneInstance.transform.localScale = Vector3.one * 1.2f;
            
            // 3. 播放缩放动画，降至 1 倍大小（假设耗时 0.8 秒，缓动曲线用 OutCubic 显得比较平滑自然）
            currentSceneInstance.transform.DOScale(Vector3.one, 2f).SetEase(Ease.OutCubic);

            // 挂载了 CanvasGroup，做透明度淡入效果
            CanvasGroup cg = currentSceneInstance.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f; // 初始全透明
                cg.DOFade(1f, 0.8f); // 0.8秒内淡入到完全不透明
            }
        }

        if (uiManager != null)
        {
            // 把当前的场景实例传给 UI Manager
            uiManager.OnSuspectChanged(suspectData, currentSceneInstance); 
        }
    }

    private void CreateTestLineup()
    {
        List<string> testIDs = new List<string>();
        var allSuspects = suspectManager.GetAllSuspects();
        int count = 0;
        foreach (var kvp in allSuspects)
        {
            testIDs.Add(kvp.Key);
            count++;
            if (count >= 4) break;
        }
        suspectManager.SetupDailyLineup(testIDs);
    }
}