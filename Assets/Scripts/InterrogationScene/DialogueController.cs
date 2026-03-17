using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Ink.Runtime; 

public class DialogueController : MonoBehaviour
{
    [Header("Controllers")]
    public InkTagController inkTagController;
    public TextController textController; 

    private Story currentStory;
    private RuntimeSuspect currentSuspect;
    
    // 未校准警告弹窗的 UI 引用
    [Header("Warning Popup UI")]
    [Tooltip("警告弹窗的父节点容器")]
    public GameObject warningPopupPanel; 
    public Button btnConfirmForce; // "是"：强行审问按钮
    public Button btnCancelForce;  // "否"：取消，等待校准按钮
    
    // 记录玩家是否已经确认过强行审问
    private bool hasIgnoredCalibration = false;
    
    private void Start()
    {
        // 初始化时隐藏警告弹窗
        if (warningPopupPanel != null) warningPopupPanel.SetActive(false);
    }

    // 1. 对话初始化与变量注入
    public void StartDialogue(RuntimeSuspect suspect)
    {
        if (suspect == null || suspect.BaseData.inkStory == null) return;

        currentSuspect = suspect;
        currentStory = new Story(suspect.BaseData.inkStory.text);
        InjectSelectedVariables(suspect.selectedQuestionVars);

        if (suspect.isInterrogated && !string.IsNullOrEmpty(suspect.currentInkState))
        {
            currentStory.state.LoadJson(suspect.currentInkState);
        }
        else
        {
            suspect.isInterrogated = true;
        }

        ContinueStory();
    }

    // 修改ink变量以匹配分析系统给出的变量
    private void InjectSelectedVariables(List<string> selectedVars)
    {
        foreach (string varName in selectedVars)
        {
            try { currentStory.variablesState[varName] = true; }
            catch (Exception e) { Debug.LogWarning($"变量注入失败: {e.Message}"); }
        }
    }

    // 2. 推进剧情与数据分发
    public void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            string text = currentStory.Continue();
            List<string> currentTags = currentStory.currentTags;

            if (inkTagController != null && currentTags != null && currentTags.Count > 0)
            {
                inkTagController.HandleTags(currentTags);
            }

            if (textController != null)
            {
                // === 新增提取嫌疑人基础音高的逻辑 ===
                float suspectPitch = (currentSuspect != null) ? currentSuspect.BaseData.typingBasePitch : 1f;

                // 传入 suspectPitch 参数
                textController.DisplayDialogue(text, false, currentTags, suspectPitch, OnTextFinishedTyping);
            }
            else
            {
                OnTextFinishedTyping();
            }
        }
        else
        {
            FinishDialogue();
        }
    }

    // 3. 选项生成与选择处理
    private void OnTextFinishedTyping()
    {
        if (currentStory.currentChoices.Count > 0)
        {
            if (textController != null)
            {
                textController.DisplayChoices(currentStory.currentChoices, OnChoiceClicked);
            }
        }
        else
        {
            ContinueStory();
        }
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        // 1. 获取 Detector 的当前状态
        DetectorController detector = inkTagController?.detectorController;
        bool isDetectorActive = detector != null && detector.gameObject.activeInHierarchy;
        bool isUncalibrated = isDetectorActive && detector.currentState == DetectorState.Uncalibrated;

        // 2. 如果检测器开启且未校准，且玩家还没无视过警告，则拦截！
        if (isUncalibrated && !hasIgnoredCalibration)
        {
            ShowCalibrationWarning(choiceIndex);
            return; // 直接 return，不往下走剧情
        }

        // 3. 正常推进剧情
        ProceedWithChoice(choiceIndex);
    }
    
    
// 👇 显示警告弹窗的逻辑
    private void ShowCalibrationWarning(int choiceIndex)
    {
        if (warningPopupPanel == null)
        {
            Debug.LogWarning("未绑定警告弹窗 UI，默认跳过拦截！");
            ProceedWithChoice(choiceIndex);
            return;
        }

        warningPopupPanel.SetActive(true);

        // 重新绑定按钮事件前，先清空旧的事件，防止重复触发
        btnConfirmForce.onClick.RemoveAllListeners();
        btnCancelForce.onClick.RemoveAllListeners();

        // 玩家选择【是】：强行审讯
        btnConfirmForce.onClick.AddListener(() => 
        {
            warningPopupPanel.SetActive(false);
            hasIgnoredCalibration = true; // 记录标志位：玩家头铁，后续不再拦截
            
            // 【新增】：玩家既然选择了强行审问，就没收他的校准按钮
            DetectorController detector = inkTagController?.detectorController;
            if (detector != null)
            {
                detector.HideCalibrateButton();
            }

            ProceedWithChoice(choiceIndex);
        });

        // 玩家选择【否】：怂了，回去校准
        btnCancelForce.onClick.AddListener(() => 
        {
            warningPopupPanel.SetActive(false);
            // 这里什么都不做。弹窗关闭，背后的对话选项依然存在，玩家可以去点检测器上的校准按钮
        });
    }
    
    // 处理玩家做出选择后的逻辑
    private void ProceedWithChoice(int choiceIndex)
    {
        if (textController != null) 
        {
            // 1. 获取玩家刚刚选中的文本内容
            string chosenText = currentStory.currentChoices[choiceIndex].text;
            
            // 2. 销毁屏幕底部的所有选项按钮
            textController.ClearChoices();

            // 3. 将玩家的选择化作一条对话，瞬间打印在屏幕上！
            // 这里强制加上 "你：" 的前缀
            // === 修改：补上玩家发言的基础音高参数 (传 1f 即可) ===
            textController.DisplayDialogue("你：" + chosenText, true, null, 1f, () =>
            {
                currentStory.ChooseChoiceIndex(choiceIndex);
                ContinueStory();
            });
        }
        else
        {
            currentStory.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }
    }

    // 4. 对话保存与结束
    public void SaveCurrentState()
    {
        if (currentSuspect != null && currentStory != null)
        {
            currentSuspect.currentInkState = currentStory.state.ToJson();
        }
    }

    private void FinishDialogue()
    {
        Debug.Log("当前对话分支已结束。");
        SaveCurrentState();
    }
}