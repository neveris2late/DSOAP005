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
    public Button continueBtn;

    [Header("Hint Settings")]
    [TextArea(2, 5)]
    public string[] hintLines;          // 在面板中输入你想要逐行显示的文本
    public float fadeDurationPerLine = 1f; // 单行文本渐显花费的时间
    public float delayBetweenLines = 0.5f; // 行与行之间的停顿时间

    // 通知 AnalysisManager 提示已结束的事件
    public event Action OnHintFinished;

    public void StartHint()
    {
        gameObject.SetActive(true);
        if (continueBtn != null) continueBtn.gameObject.SetActive(false);
        if (hintTextBox != null) hintTextBox.text = "";
        
        StartCoroutine(FadeLinesRoutine());
    }

    private IEnumerator FadeLinesRoutine()
    {
        string currentText = ""; // 用于累加已经完全显示出来的文本
        
        for (int i = 0; i < hintLines.Length; i++)
        {
            float alpha = 0;
            string currentLine = hintLines[i];
            
            // 使用 DOTween 动态改变富文本的 Alpha Hex 值，实现当前行的渐显
            Tween t = DOTween.To(() => alpha, x => {
                alpha = x;
                int alphaHex = (int)(alpha * 255);
                string hexStr = alphaHex.ToString("X2"); // 转换为两位的十六进制
                // 拼接之前的文本 + 正在渐显的当前行
                hintTextBox.text = currentText + $"<alpha=#{hexStr}>{currentLine}";
            }, 1f, fadeDurationPerLine);

            yield return t.WaitForCompletion(); // 等待这行文本完全显现
            
            // 这一行完全显示后，将其固化并加上换行符
            currentText += currentLine + "\n";
            hintTextBox.text = currentText; 
            
            yield return new WaitForSeconds(delayBetweenLines); // 停顿一会儿再显示下一行
        }

        // 所有文本显示完毕，激活 Continue 按钮
        if (continueBtn != null) 
        {
            continueBtn.gameObject.SetActive(true);
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => {
                gameObject.SetActive(false); // 玩家点击后失活 HintPanel
                OnHintFinished?.Invoke();    // 发送事件通知 AM
            });
        }
    }
}