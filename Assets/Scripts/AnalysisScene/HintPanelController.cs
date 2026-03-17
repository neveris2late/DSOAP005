using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class HintPanelController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("主提示文本的打字机组件 (将使用逐行瞬间显示)")]
    public GlitchTypeWriterEffect mainHintWriter;
    [Tooltip("副提示文本的打字机组件 (将使用逐行乱序打字)")]
    public GlitchTypeWriterEffect subHintWriter;
    
    public Button continueBtn; 

    [Header("Hint Settings")]
    [Tooltip("行与行之间停留阅读的时间")]
    public float delayBetweenLines = 0.5f; 

    public event Action OnHintFinished;

    private void Awake()
    {
        // 【新增】：监听继续按钮的点击事件
        if (continueBtn != null)
        {
            continueBtn.onClick.AddListener(OnContinueClicked);
        }
    }

    public void StartHint()
    {
        gameObject.SetActive(true);
        // 开始时先隐藏继续按钮
        if (continueBtn != null) continueBtn.gameObject.SetActive(false); 

        StartCoroutine(HintFlowRoutine());
    }

    private IEnumerator HintFlowRoutine()
    {
        // 1. 直接触发播放，读取文本框自带的文字
        if (mainHintWriter != null) 
        {
            mainHintWriter.PlayLineByLine(delayBetweenLines);
        }

        if (subHintWriter != null) 
        {
            subHintWriter.PlayLineByLineGlitch(delayBetweenLines);
        }

        // 2. 持续等待，直到两个打字机都不在播放状态
        while ((mainHintWriter != null && mainHintWriter.IsPlaying) || 
               (subHintWriter != null && subHintWriter.IsPlaying))
        {
            yield return null;
        }

        // 3. 【修改】：播放完毕后，不再直接关闭面板，而是激活继续按钮让玩家点击
        if (continueBtn != null)
        {
            continueBtn.gameObject.SetActive(true);
        }
        else
        {
            // 防御性代码：如果没有绑定按钮，只能直接结束
            FinishHintFlow();
        }
    }

    // 【新增】：玩家点击继续按钮时触发
    private void OnContinueClicked()
    {
        FinishHintFlow();
    }

    // 【新增】：抽离出的结束逻辑，关闭面板并通知 AnalysisManager
    private void FinishHintFlow()
    {
        gameObject.SetActive(false);
        OnHintFinished?.Invoke();
    }

    private void OnDestroy()
    {
        // 移除监听防内存泄漏
        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveListener(OnContinueClicked);
        }
    }
}