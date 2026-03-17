using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;  
using DG.Tweening; 

public class InterrogationSceneUiManager : MonoBehaviour
{
    [Header("UI 数据展示引用")]
    public TextMeshProUGUI suspectNameText; 
    public TextMeshProUGUI hintTextUI; 

    [Header("UI 动画控制引用")]
    public List<ParentAnimController> uiAnimGroups;
    public float totalEnterAnimDuration = 1.5f; 
    public ParentAnimController hintContainerAnimGroup; 

    [Header("对话交互引用")]
    public GameObject dialogueBtnContainer; 
    public Button dialogueBtn;            
    public Image dialoguePortraitImage; 
    
    [Header("判定结果提示UI")]
    [Tooltip("包含判定结果文字的背景/边框容器")]
    public GameObject judgmentResultContainer; 
    [Tooltip("显示判定结果的文本组件")]
    public TextMeshProUGUI judgmentResultText;
    
    [Tooltip("对话按钮的父节点动画控制器，用于播放出入场动画")]
    public ParentAnimController dialogueBtnAnimGroup; 
    
    [Tooltip("预估对话按钮退出动画的耗时，等播完再隐藏节点")]
    public float btnExitAnimDuration = 0.5f;
    
    [Tooltip("预估提示文本退出动画的耗时，等播完再显示对话按钮")]
    public float hintExitAnimDuration = 0.5f;
    
    [Header("新UI 引用")]
    [Tooltip("新对话系统的 Scroll View 节点")]
    public GameObject dialogueScrollView;

    // ==========================================
    // 检测器 (Detector) 引用
    // ==========================================
    [Header("检测器 (Detector) 引用")]
    [Tooltip("DetectorContainer 的游戏物体，用于控制整体显隐")]
    public GameObject detectorContainer; 
    [Tooltip("DetectorContainer 上挂载的父节点动画控制器")]
    public ParentAnimController detectorAnimGroup;
    
    // ==========================================
    // 判定系统引用
    // ==========================================
    [Header("判定系统引用")]
    [Tooltip("场景中呼出判定面板的入口按钮")]
    public Button judgmentBtn; 
    [Tooltip("负责处理判定逻辑的控制器")]
    public JudgmentController judgmentController;
    
    // 获取对话控制器的引用
    [Tooltip("负责驱动 Ink 剧本的核心控制器")]
    public DialogueController dialogueController;

    [Header("UI 音效配置")]
    [Tooltip("整体界面入场时播放的音效（例如：系统启动声）")]
    public AudioClip uiEnterSound;
    
    [Tooltip("提示文本渐显时的音效（例如：低沉的数据扫描或打字声）")]
    public AudioClip hintTextAppearSound;
    
    [Tooltip("对话按钮弹出时的音效")]
    public AudioClip dialogueBtnAppearSound;
    
    [Tooltip("点击对话按钮时的确认音效（例如：清脆的高科技点击声）")]
    public AudioClip dialogueBtnClickSound;

    // 缓存数据
    private RuntimeSuspect currentSuspectData;
    private GameObject currentSceneInstance; 

    void Start()
    {
        if (dialogueBtn != null)
        {
            dialogueBtn.onClick.AddListener(OnDialogueButtonClicked);
        }
        
        // 注册判定按钮的点击事件
        if (judgmentBtn != null)
        {
            judgmentBtn.onClick.AddListener(OnJudgmentButtonClicked);
        }

        if (dialoguePortraitImage != null)
        {
            dialoguePortraitImage.gameObject.SetActive(false);
        }

        // 【新增】：在游戏开始时强制隐藏判定结果容器
        if (judgmentResultContainer != null)
        {
            judgmentResultContainer.SetActive(false);
        }
    }

    public void OnSuspectChanged(RuntimeSuspect suspectData, GameObject sceneInstance)
    {
        currentSuspectData = suspectData;
        currentSceneInstance = sceneInstance;
        
        // 【新增】：切换嫌疑人时，清空上一人的对话记录，并先隐藏对话框
        if (dialogueController != null && dialogueController.textController != null)
        {
            dialogueController.textController.ClearDialogueHistory();
        }
        if (dialogueScrollView != null)
        {
            dialogueScrollView.SetActive(false);
        }
        
        // 【修改点1】：在初始加载时强制隐藏判定按钮
        if (judgmentBtn != null) judgmentBtn.gameObject.SetActive(false);

        // 1. 更新基础数据和重置状态
        if (suspectNameText != null) suspectNameText.text = suspectData.BaseData.suspectName;
        
        // 初始阶段隐藏对话按钮容器，等待提示文本结束后再激活
        if (dialogueBtnContainer != null) dialogueBtnContainer.SetActive(false); 
        else if (dialogueBtn != null) dialogueBtn.gameObject.SetActive(false); 
        
        if (dialogueBtn != null) dialogueBtn.interactable = false; // 禁用交互

        if (dialoguePortraitImage != null) dialoguePortraitImage.gameObject.SetActive(false); 

        // 【新增】初始阶段隐藏 DetectorContainer
        if (detectorContainer != null) detectorContainer.SetActive(false);

        // 2. 初始化提示文本内容
        if (hintTextUI != null)
        {
            hintTextUI.text = suspectData.BaseData.enterHintText;
            hintTextUI.alpha = 0f; 
        }

        // 3. 播放其他常规UI入场动画
        PlayAllUIEnterAnimations();

        // 4. 定时执行文本渐显和退场
        DOVirtual.DelayedCall(totalEnterAnimDuration, ShowHintTextAndScheduleExit);
    }
    
    
    // ==========================================
    // 【新增】判定按钮相关逻辑
    // ==========================================

    /// <summary>
    /// 显示判定按钮（可以在对话结束的回调中调用此方法）
    /// </summary>
    public void ShowJudgmentButton()
    {
        if (judgmentBtn != null)
        {
            judgmentBtn.gameObject.SetActive(true);
            
            // 可选：如果你有专用的入场动画控制器也可以在这里播放
            // judgmentBtnAnimGroup.PlayEnter(); 
        }
    }
    
    private void OnJudgmentButtonClicked()
    {
        if (currentSuspectData == null || judgmentController == null) return;

        // 播放点击音效
        if (AudioManager.Instance != null && dialogueBtnClickSound != null)
        {
            AudioManager.Instance.PlayUISFX(dialogueBtnClickSound);
        }

        // 调用 JudgmentController 打开卡片面板
        judgmentController.OpenJudgmentPanel(currentSuspectData);

        // 点击后隐藏判定按钮自身，防止重复点击
        judgmentBtn.gameObject.SetActive(false);
        
        // 如果对话立绘等还在显示，也可以在这里将其淡出隐藏，腾出屏幕空间给判定卡片
    }
    
    public void ShowResultPrompt(string message)
    {
        // 如果没有文本内容，或者没有绑定UI组件，则不执行
        if (string.IsNullOrEmpty(message) || judgmentResultText == null || judgmentResultContainer == null) 
            return;

        judgmentResultText.text = message;
        judgmentResultContainer.SetActive(true); // 显示容器
        judgmentResultText.alpha = 0f; // 初始全透明

        // 停止之前的动画防止重叠（如果玩家快速按9）
        judgmentResultText.DOKill();

        // 淡入淡出演出
        judgmentResultText.DOFade(1f, 0.5f).OnComplete(() =>
        {
            // 停留 4 秒后淡出
            judgmentResultText.DOFade(0f, 0.5f).SetDelay(4f).OnComplete(() => 
            {
                // 彻底隐藏容器
                judgmentResultContainer.SetActive(false);
            });
        });
    }

    private void PlayAllUIEnterAnimations()
    {
        if (AudioManager.Instance != null && uiEnterSound != null)
        {
            AudioManager.Instance.PlayUISFX(uiEnterSound);
        }

        foreach (var animGroup in uiAnimGroups)
        {
            if (animGroup != null) animGroup.PlayEnter();
        }
    }

    private void ShowHintTextAndScheduleExit()
    {
        if (hintTextUI != null)
        {
            if (AudioManager.Instance != null && hintTextAppearSound != null)
            {
                AudioManager.Instance.PlayUISFX(hintTextAppearSound);
            }

            hintTextUI.DOFade(1f, 1f).OnComplete(() => 
            {
                DOVirtual.DelayedCall(2.5f, () => 
                {
                    if (hintContainerAnimGroup != null)
                    {
                        hintContainerAnimGroup.PlayExit();
                    }
                    else
                    {
                        hintTextUI.DOFade(0f, hintExitAnimDuration);
                    }

                    DOVirtual.DelayedCall(hintExitAnimDuration, ShowAndAnimateDialogueButton);
                });
            });
        }
        else
        {
            ShowAndAnimateDialogueButton();
        }
    }

    private void ShowAndAnimateDialogueButton()
    {
        if (dialogueBtnContainer != null) dialogueBtnContainer.SetActive(true);
        else if (dialogueBtn != null) dialogueBtn.gameObject.SetActive(true);

        if (dialogueBtn != null) dialogueBtn.interactable = true;

        if (AudioManager.Instance != null && dialogueBtnAppearSound != null)
        {
            AudioManager.Instance.PlayUISFX(dialogueBtnAppearSound);
        }

        if (dialogueBtnAnimGroup != null)
        {
            dialogueBtnAnimGroup.PlayEnter();
        }
    }

    private void OnDialogueButtonClicked()
    {
        if (currentSuspectData == null) return;

        if (AudioManager.Instance != null && dialogueBtnClickSound != null)
        {
            AudioManager.Instance.PlayUISFX(dialogueBtnClickSound);
        }

        // 1. 立即禁用按钮
        if (dialogueBtn != null) dialogueBtn.interactable = false;

        // 2. 播放按钮退出动画并隐藏
        if (dialogueBtnAnimGroup != null)
        {
            dialogueBtnAnimGroup.PlayExit(); 
            
            DOVirtual.DelayedCall(btnExitAnimDuration, () => 
            {
                if (dialogueBtnContainer != null) dialogueBtnContainer.SetActive(false);
                else if (dialogueBtn != null) dialogueBtn.gameObject.SetActive(false);
            });
        }
        else
        {
            if (dialogueBtnContainer != null) dialogueBtnContainer.SetActive(false);
            else if (dialogueBtn != null) dialogueBtn.gameObject.SetActive(false);
        }

        // 3. 场景立绘切换动画
        if (currentSceneInstance != null)
        {
            Transform workPortrait = currentSceneInstance.transform.Find("WorkPortraitObj");
            if (workPortrait != null) workPortrait.gameObject.SetActive(false);
            
            Transform backgroundPortrait = currentSceneInstance.transform.Find("SuspectBackground");
            if (backgroundPortrait != null)
            {
                Vector3 targetScale = backgroundPortrait.localScale * 1.05f; 
                backgroundPortrait.DOScale(targetScale, 0.5f).SetEase(Ease.OutCubic);

                Image bgImage = backgroundPortrait.GetComponent<Image>();
                if (bgImage != null) bgImage.DOFade(0.4f, 0.5f); 
            }
        }

        // 4. 显示正面立绘
        if (dialoguePortraitImage != null && currentSuspectData.BaseData.dialoguePortrait != null) 
        {
            dialoguePortraitImage.sprite = currentSuspectData.BaseData.dialoguePortrait;
            dialoguePortraitImage.gameObject.SetActive(true);
            
            dialoguePortraitImage.color = new Color(1, 1, 1, 0); 
            dialoguePortraitImage.DOFade(1f, 0.5f);
        }
        
        // 5. 显示判定按钮
        ShowJudgmentButton();
        
        // =====================================
        // 【新增】：6. 淡入显示对话的 Scroll View 
        // =====================================
        if (dialogueScrollView != null)
        {
            dialogueScrollView.SetActive(true);
            
            // 如果你给 Scroll View 挂了 CanvasGroup，可以加个渐显动画，更有高级感
            CanvasGroup scrollCG = dialogueScrollView.GetComponent<CanvasGroup>();
            if (scrollCG != null)
            {
                scrollCG.alpha = 0f;
                scrollCG.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            }
        }
        
        // =====================================
        // 7. 正式启动 Ink 对话！
        // =====================================
        if (dialogueController != null)
        {
            dialogueController.StartDialogue(currentSuspectData);
        }
        else
        {
            Debug.LogError("[UI Manager] 对话无法开始，因为没有绑定 DialogueController！");
        }
        
        
    }
}