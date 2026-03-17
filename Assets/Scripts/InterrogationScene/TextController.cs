using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // 引入正则

// 新增：情绪配置结构体
[Serializable]
public struct MoodConfig
{
    public string moodTag;        // 如: calm, angry, dread, nervous
    public float speedMultiplier; // 速度倍率 (例如: 1是正常, 0.5是两倍速, 2是慢速)
    public AudioClip typingSound; // 对应情绪的打字机音效
    // === 修改：改为倍率 ===
    [Tooltip("音高倍率：与嫌疑人的基础Pitch相乘。默认为1（不改变基础音高）")]
    [Range(0.5f, 2f)] 
    public float pitchMultiplier;
}

public class TextController : MonoBehaviour
{
    [Header("Scroll View 引用")]
    public ScrollRect scrollRect;
    public RectTransform contentRect;
    
    [Header("Prefabs")]
    public GameObject dialogueLinePrefab;
    public GameObject choiceButtonPrefab;

    private List<GameObject> activeChoiceButtons = new List<GameObject>(); 
    
    [Header("打字机情绪与音频设置")]
    [Tooltip("全局统一的对话音效播放器")]
    public MoodConfig defaultMood = new MoodConfig { moodTag = "default", speedMultiplier = 1f };
    public List<MoodConfig> moodConfigs; // 在 Inspector 中配置各个档位

    // 新增 isPlayer 参数
    public void DisplayDialogue(string text, bool isPlayer, List<string> tags, float characterBasePitch, Action onComplete)
    {
        GameObject newLineObj = Instantiate(dialogueLinePrefab, contentRect);
        TextMeshProUGUI tmpText = newLineObj.GetComponent<TextMeshProUGUI>();
        RichTextTypewriter typewriter = newLineObj.GetComponent<RichTextTypewriter>();

        string formattedText = ParseActionAndDialogue(text, isPlayer);
        
        MoodConfig currentMood = defaultMood;
        if (!isPlayer && tags != null)
        {
            foreach (string tag in tags)
            {
                MoodConfig foundMood = moodConfigs.Find(m => m.moodTag == tag);
                if (!string.IsNullOrEmpty(foundMood.moodTag))
                {
                    currentMood = foundMood;
                    break; 
                }
            }
        }
        
        typewriter.currentSpeedMultiplier = currentMood.speedMultiplier;

        // === 核心逻辑：计算最终音高 ===
        // 如果是玩家说话，默认音高为 1f；如果是嫌疑人，则为：基础音高 * 情绪倍率
        float finalPitch = isPlayer ? 1f : (characterBasePitch * currentMood.pitchMultiplier);
        
        // 传入音效和计算后的最终音高
        typewriter.PlayText(formattedText, isPlayer, currentMood.typingSound, finalPitch);
        
        ScrollToBottom();
        
        if (isPlayer)
        {
            StartCoroutine(WaitAFrameAndComplete(onComplete));
        }
        else
        {
            StartCoroutine(WaitForTypewriter(typewriter, () => {
                ScrollToBottom();
                onComplete?.Invoke();
            }));
        }
    }
    
    // 【新增】：专为玩家文本准备的缓冲协程
    private IEnumerator WaitAFrameAndComplete(Action onComplete)
    {
        // 强制立即重绘当前的 Canvas 排版
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        
        // 等待下一帧，确保排版万无一失
        yield return null; 
        
        onComplete?.Invoke();
    }

    private string ParseActionAndDialogue(string rawText, bool isPlayer)
    {
        string speakerName = "";
        string content = rawText;
        
        // 1. 尝试提取说话人名字 (寻找第一个冒号)
        int colonIndex = rawText.IndexOf('：');
        if (colonIndex == -1) colonIndex = rawText.IndexOf(':');
        
        if (colonIndex != -1 && colonIndex < 15) // 限制名字长度，防止误判
        {
            speakerName = rawText.Substring(0, colonIndex + 1);
            content = rawText.Substring(colonIndex + 1).Trim();
        }

        // 2. 玩家文本配色 (冷峻的赛博青绿与灰蓝调)
        if (isPlayer)
        {
            string pNameColor = "#00FF9D"; // 荧光青绿
            string pTextColor = "#8A9EA7"; // 灰冷调，代表记录已过去
            return $"<color={pNameColor}><b>{speakerName}</b></color><color={pTextColor}>{content}</color>";
        }

        // 3. 嫌疑人文本配色 
        string sNameColor = "#00E5FF";     // 亮蓝色 (名字)
        string actionColor = "#B388FF";    // 浅紫色 (动作描写与神态)
        string dialogueColor = "#FFFFFF";  // 纯白色 (说出的对白)

        // 核心正则：匹配被引号包裹的所有内容，将其染成对白颜色，其余部分染成动作颜色
        string formattedContent = Regex.Replace(content, @"([“「""][^”」""]*[”」""])", $"</color><color={dialogueColor}>$1</color><color={actionColor}>");
        
        // 包裹整体
        formattedContent = $"<color={actionColor}>{formattedContent}</color>";
        // 清理由于正则可能产生的空标签
        formattedContent = formattedContent.Replace($"<color={actionColor}></color>", ""); 
        formattedContent = formattedContent.Replace($"<color={dialogueColor}></color>", "");

        string finalResult = "";
        if (!string.IsNullOrEmpty(speakerName))
        {
            finalResult += $"<color={sNameColor}><b>{speakerName}</b></color>";
        }
        finalResult += formattedContent;

        return finalResult;
    }

    private IEnumerator WaitForTypewriter(RichTextTypewriter typewriter, Action onComplete)
    {
        while (typewriter.IsPlaying)
        {
            ScrollToBottom();
            yield return null;
        }
        onComplete?.Invoke();
    }

    public void DisplayChoices(List<Ink.Runtime.Choice> choices, Action<int> onChoiceClicked)
    {
        ClearChoices(); 

        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            GameObject btnObj = Instantiate(choiceButtonPrefab, contentRect);
            activeChoiceButtons.Add(btnObj);

            var textUI = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            
            // ==========================================
            // 【优雅方案】：利用正则捕获隐藏标签
            // ==========================================
            string rawText = choice.text; 
            string displayTitle = rawText; 
            string hoverTag = "";          

            // 精准匹配形如 <hover:施加压力> 或 <hover：施加压力> 的内容
            Match match = Regex.Match(rawText, @"<hover[:：](.*?)>");
            if (match.Success)
            {
                // 提取属性名，例如 "施加压力"
                hoverTag = match.Groups[1].Value.Trim();
                
                // 将整个 <hover:xxx> 标签从原文字中抹除，不留痕迹
                displayTitle = rawText.Replace(match.Value, "").Trim();
            }
            
            // UI 上只显示干净的文字："1. 我也可以直接逮捕你"
            textUI.text = $"{i + 1}. {displayTitle}";

            // 挂载并初始化悬停脚本
            ChoiceHoverController hoverEffect = btnObj.GetComponent<ChoiceHoverController>();
            if (hoverEffect == null) hoverEffect = btnObj.AddComponent<ChoiceHoverController>();
            
            hoverEffect.InitializeByString(hoverTag); 

            Button btn = btnObj.GetComponent<Button>();
            int index = choice.index;
            btn.onClick.AddListener(() => onChoiceClicked?.Invoke(index));
        }
        ScrollToBottom();
    }

    public void ClearChoices()
    {
        foreach (var btn in activeChoiceButtons)
        {
            if (btn != null) 
            {
                // 【关键修复】：立刻隐藏物体，让它瞬间脱离 Layout Group 的排版计算！
                btn.SetActive(false); 
                Destroy(btn);
            }
        }
        activeChoiceButtons.Clear();
    }

    public void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomRoutine());
    }

    private IEnumerator ScrollToBottomRoutine()
    {
        yield return new WaitForEndOfFrame();
        if (contentRect != null && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    // ==========================================
    // 新增：清空当前屏幕上的所有对话和选项
    // ==========================================
    public void ClearDialogueHistory()
    {
        // 1. 先清空底部可能残留的选项按钮
        ClearChoices(); 

        // 2. 遍历 Content 下所有的子物体（历史对话行），全部销毁
        if (contentRect != null)
        {
            foreach (Transform child in contentRect)
            {
                Destroy(child.gameObject);
            }
        }
    }
}