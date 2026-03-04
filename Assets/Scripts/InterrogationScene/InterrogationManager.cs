using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class InterrogationManager : MonoBehaviour
{
    private RuntimeSuspectManager suspectManager;
    
    [Header("场景与UI引用")]
    public Transform sceneContainer; 
    public InterrogationSceneUiManager uiManager;

    private GameObject currentSceneInstance;

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
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSuspectIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSuspectIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSuspectIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSuspectIndex(3);
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

// 需要确保文件顶部有 using DG.Tweening;

    private void HandleSuspectDataDistributed(RuntimeSuspect suspectData)
    {
        if (currentSceneInstance != null) Destroy(currentSceneInstance);

        if (suspectData.BaseData.interrogationScenePrefab != null)
        {
            // 1. 实例化 Prefab
            currentSceneInstance = Instantiate(suspectData.BaseData.interrogationScenePrefab, sceneContainer);

            // === 【新增】场景实例入场动画 ===
            
            // 2. 设置初始缩放为 1.2 倍
            currentSceneInstance.transform.localScale = Vector3.one * 1.2f;
            
            // 3. 播放缩放动画，降至 1 倍大小（假设耗时 0.8 秒，缓动曲线用 OutCubic 显得比较平滑自然）
            currentSceneInstance.transform.DOScale(Vector3.one, 2f).SetEase(Ease.OutCubic);

            //挂载了 CanvasGroup，想同时做透明度淡入效果，可以解除下面这段代码的注释：

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