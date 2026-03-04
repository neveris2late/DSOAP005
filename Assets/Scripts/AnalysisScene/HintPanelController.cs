using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System;

public class HintPanelController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI hintTextBox;
    // 保留此引用以防你的 Inspector 丢失绑定，但在这个新逻辑中我们默认将其隐藏
    public Button continueBtn; 

    [Header("Hint Settings")]
    [TextArea(2, 5)]
    public string[] hintLines;          // 在面板中输入你想要逐行显示的文本
    public float fadeDurationPerLine = 1f; // 单行文本渐显/渐隐花费的时间
    public float delayBetweenLines = 0.5f; // 行与行之间停留阅读的时间

    // 通知 AnalysisManager 提示已结束的事件
    public event Action OnHintFinished;

    private bool skipRequested = false; // 用于标记玩家是否点击了屏幕

    public void StartHint()
    {
        gameObject.SetActive(true);
        if (continueBtn != null) continueBtn.gameObject.SetActive(false); // 隐藏继续按钮
        
        // 初始状态下将文本透明度设为0
        Color c = hintTextBox.color;
        c.a = 0;
        hintTextBox.color = c;
        
        StartCoroutine(FadeLinesRoutine());
    }

    private void Update()
    {
        // 监听鼠标左键点击或屏幕触摸
        if (Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
    }

    private IEnumerator FadeLinesRoutine()
    {
        for (int i = 0; i < hintLines.Length; i++)
        {
            // 每次只显示当前行
            hintTextBox.text = hintLines[i];
            skipRequested = false; // 重置跳过标记

            // --- 阶段 1：渐显 (Fade In) ---
            Tween fadeIn = hintTextBox.DOFade(1f, fadeDurationPerLine);
            while (fadeIn.IsActive() && !fadeIn.IsComplete())
            {
                if (skipRequested)
                {
                    fadeIn.Complete(); // 玩家点击，瞬间完成渐显动画
                    skipRequested = false;
                    break;
                }
                yield return null;
            }

            // --- 阶段 2：停留阅读 (Wait) ---
            float waitTimer = 0f;
            while (waitTimer < delayBetweenLines)
            {
                if (skipRequested)
                {
                    skipRequested = false;
                    break; // 玩家点击，跳过等待时间
                }
                waitTimer += Time.deltaTime;
                yield return null;
            }

            // --- 阶段 3：渐隐 (Fade Out) ---
            Tween fadeOut = hintTextBox.DOFade(0f, fadeDurationPerLine);
            while (fadeOut.IsActive() && !fadeOut.IsComplete())
            {
                if (skipRequested)
                {
                    fadeOut.Complete(); // 玩家点击，瞬间完成渐隐动画
                    skipRequested = false;
                    break;
                }
                yield return null;
            }
        }

        // 所有行播放完毕，直接关闭面板并触发回调
        gameObject.SetActive(false);
        OnHintFinished?.Invoke();
    }
}