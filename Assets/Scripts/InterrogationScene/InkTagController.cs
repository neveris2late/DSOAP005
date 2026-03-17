using System.Collections.Generic;
using UnityEngine;

public class InkTagController : MonoBehaviour
{
    [Header("Core Controllers")]
    [Tooltip("负责读取文本与选项的主逻辑")]
    public DialogueController dialogueController;
    
    [Tooltip("负责打字机效果、文字颜色与音效呈现")]
    public TextController textController;
    
    [Tooltip("负责审问环节检测条")]
    public DetectorController detectorController;
    
    // 检测器的整体父节点容器
    [Tooltip("检测器的整体父节点（用于控制整体显隐）")]
    public GameObject detectorContainer;

    // ==========================================
    // 核心方法：由 DialogueController 在读取到新一行文本时调用
    // ==========================================
    public void HandleTags(List<string> currentTags)
    {
        // 如果当前行没有标签，直接跳过
        if (currentTags == null || currentTags.Count == 0) return;

        foreach (string tag in currentTags)
        {
            ParseAndExecuteTag(tag);
        }
    }

    // ==========================================
    // 标签解析与分发逻辑
    // ==========================================
    private void ParseAndExecuteTag(string tagText)
    {
        // 标签格式例如 "# fill:80" 或 "# state:nervous"
        // 以冒号为分隔符，拆分指令和参数
        string[] splitTag = tagText.Split(':');
        string command = splitTag[0].Trim().ToLower(); // 统一转为小写
        string parameter = splitTag.Length > 1 ? splitTag[1].Trim() : string.Empty;

        switch (command)
        {
            // 1. 审问检测器 - 填充度指令 (例如: # fill:80)
            case "fill":
                HandleFillTag(parameter);
                break;

            // 2. 审问检测器 - 状态指令 (例如: # state:nervous)
            case "state":
                HandleStateTag(parameter);
                break;

            // 3. 文本显示速度指令 (例如: # speed:0.05)
            case "speed":
                HandleTextSpeedTag(parameter);
                break;

            // 4. 文本颜色指令 (例如: # color:yellow)
            case "color":
                HandleTextColorTag(parameter);
                break;
                
            // 5. 打字音效切换 (例如: # voice:robot)
            case "voice":
                HandleVoiceTag(parameter);
                break;
            
            // 【新增】开启检测器指令 (例如: # opendetector)
            case "opendetector":
                HandleOpenDetectorTag();
                break;

            default:
                Debug.LogWarning($"[InkTagController] 接收到未处理的标签: {command}");
                break;
        }
    }

    // ==========================================
    // 具体指令的执行方法
    // ==========================================
    
    /// <summary>
    /// 处理检测器开启演出指令
    /// </summary>
    private void HandleOpenDetectorTag()
    {
        if (detectorController == null)
        {
            Debug.LogWarning("[InkTagController] DetectorController 未绑定，无法播放开启演出！");
            return;
        }

        // 1. 优先激活整个容器（如果绑定了的话）
        if (detectorContainer != null)
        {
            detectorContainer.SetActive(true);
        }
        else
        {
            // 兜底方案：万一你没绑容器，就只激活它自己
            detectorController.gameObject.SetActive(true);
        }

        // 2. 播放入场动画 (如果有 ParentAnimController)
        // 注意：如果你把入场动画挂在了 Container 上，这里也需要改成从 detectorContainer 上获取组件
        var animGroup = (detectorContainer != null) ? 
            detectorContainer.GetComponent<ParentAnimController>() : 
            detectorController.GetComponent<ParentAnimController>();
            
        if (animGroup != null) animGroup.PlayEnter();

        // 3. 调用检测器内部的开启填充演出
        detectorController.PlayOpenAnimation();
    }
    
    /// <summary>
    /// 处理检测器填充度，接受 0 - 100 的数值
    /// </summary>
    private void HandleFillTag(string param)
    {
        if (detectorController == null)
        {
            Debug.LogWarning("[InkTagController] DetectorController 未绑定！");
            return;
        }

        // 尝试将字符串参数转换为浮点数 (0 - 100)
        if (float.TryParse(param, out float fillValue))
        {
            // 将 0-100 的数值转换为 0.0 - 1.0 的比例，适配现有的 DetectorController
            float normalizedValue = fillValue / 100f;
            detectorController.SetFillAmount(normalizedValue);
        }
        else
        {
            Debug.LogError($"[InkTagController] 无法解析 Fill 标签的值: {param}");
        }
    }

    /// <summary>
    /// 处理检测器状态切换
    /// </summary>
    private void HandleStateTag(string param)
    {
        if (detectorController == null)
        {
            Debug.LogWarning("[InkTagController] DetectorController 未绑定！");
            return;
        }

        string stateName = param.ToLower();
        
        switch (stateName)
        {
            case "uncalibrated":
                Debug.Log("[InkTagController] 切换检测器状态 -> 未校准 (Uncalibrated)");
                detectorController.SetState(DetectorState.Uncalibrated);
                break;
                
            case "calm":         
                Debug.Log("[InkTagController] 切换检测器状态 -> 平静 (Calm)");
                detectorController.SetState(DetectorState.Calm);
                break;
                
            case "color_changed":
                Debug.Log("[InkTagController] 切换检测器状态 -> 变色 (Color Changed)");
                detectorController.SetState(DetectorState.ColorChanged);
                break;
                
            case "nervous":      
                Debug.Log("[InkTagController] 切换检测器状态 -> 紧张 (Nervous)");
                detectorController.SetState(DetectorState.Nervous);
                break;
                
            default:
                Debug.LogWarning($"[InkTagController] 未知的检测器状态标签: {stateName}");
                break;
        }
    }

    // --- 文本表现层占位方法 ---

    private void HandleTextSpeedTag(string param)
    {
        if (textController == null) return;
        if (float.TryParse(param, out float speedValue))
        {
            // textController.SetTypingSpeed(speedValue);
        }
    }

    private void HandleTextColorTag(string param)
    {
        if (textController == null) return;
        // textController.SetTextColor(param);
    }
    
    private void HandleVoiceTag(string param)
    {
        if (textController == null) return;
        // textController.SetTypingSoundProfile(param);
    }
}