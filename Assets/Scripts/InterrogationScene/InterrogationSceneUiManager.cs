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
    
    [Tooltip("对话按钮的父节点动画控制器，用于播放出入场动画")]
    public ParentAnimController dialogueBtnAnimGroup; 
    
    [Tooltip("预估对话按钮退出动画的耗时，等播完再隐藏节点")]
    public float btnExitAnimDuration = 0.5f;
    
    [Tooltip("预估提示文本退出动画的耗时，等播完再显示对话按钮")]
    public float hintExitAnimDuration = 0.5f;

    // 缓存数据
    private RuntimeSuspect currentSuspectData;
    private GameObject currentSceneInstance; 

    void Start()
    {
        if (dialogueBtn != null)
        {
            dialogueBtn.onClick.AddListener(OnDialogueButtonClicked);
        }

        if (dialoguePortraitImage != null)
        {
            dialoguePortraitImage.gameObject.SetActive(false);
        }
    }

    public void OnSuspectChanged(RuntimeSuspect suspectData, GameObject sceneInstance)
    {
        currentSuspectData = suspectData;
        currentSceneInstance = sceneInstance; 

        // 1. 更新基础数据和重置状态
        if (suspectNameText != null) suspectNameText.text = suspectData.BaseData.suspectName;
        
        // 【核心修改1】初始阶段隐藏对话按钮容器，等待提示文本结束后再激活
        if (dialogueBtnContainer != null) dialogueBtnContainer.SetActive(false); 
        else if (dialogueBtn != null) dialogueBtn.gameObject.SetActive(false); 
        
        if (dialogueBtn != null) dialogueBtn.interactable = false; // 禁用交互

        if (dialoguePortraitImage != null) dialoguePortraitImage.gameObject.SetActive(false); 

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

    private void PlayAllUIEnterAnimations()
    {
        foreach (var animGroup in uiAnimGroups)
        {
            if (animGroup != null) animGroup.PlayEnter();
        }
    }

    private void ShowHintTextAndScheduleExit()
    {
        if (hintTextUI != null)
        {
            // 文本渐显入场
            hintTextUI.DOFade(1f, 1f).OnComplete(() => 
            {
                // 等待 2.5 秒后开始退场
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

                    // 【核心修改2】利用 DOTween 的延迟调用，在提示文本退场动画播放完毕后，加载并播放对话按钮的入场动画
                    DOVirtual.DelayedCall(hintExitAnimDuration, ShowAndAnimateDialogueButton);
                });
            });
        }
        else
        {
            // 兜底逻辑：如果当前嫌疑人没有提示文本，直接显示对话按钮
            ShowAndAnimateDialogueButton();
        }
    }

    // 【新增方法】用于专门处理对话按钮的激活与入场动画
    private void ShowAndAnimateDialogueButton()
    {
        // DOTween 动画通常需要物体处于 Active 状态才能正常运行，因此先 SetActive(true)
        if (dialogueBtnContainer != null) dialogueBtnContainer.SetActive(true);
        else if (dialogueBtn != null) dialogueBtn.gameObject.SetActive(true);

        if (dialogueBtn != null) dialogueBtn.interactable = true;

        // 播放按钮的入场动画
        if (dialogueBtnAnimGroup != null)
        {
            dialogueBtnAnimGroup.PlayEnter();
        }
    }

    private void OnDialogueButtonClicked()
    {
        if (currentSuspectData == null) return;

        // 1. 立即禁用按钮，防止动画播放期间被重复点击
        if (dialogueBtn != null) dialogueBtn.interactable = false;

        // 2. 播放按钮的退出动画，并延迟隐藏节点
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

        // 3. 隐藏工作立绘，寻找场景中的背景立绘，并播放放大和半透明动画
        if (currentSceneInstance != null)
        {
            Transform workPortrait = currentSceneInstance.transform.Find("WorkPortraitObj");
            if (workPortrait != null)
            {
                workPortrait.gameObject.SetActive(false);
            }
            // 查找名为 "SuspectBackground" 的节点
            Transform backgroundPortrait = currentSceneInstance.transform.Find("SuspectBackground");
            if (backgroundPortrait != null)
            {
                // 【动画1：放大】基于原本的 scale 放大 1.05 到 1.1 倍，耗时 0.5 秒
                Vector3 targetScale = backgroundPortrait.localScale * 1.05f; 
                backgroundPortrait.DOScale(targetScale, 0.5f).SetEase(Ease.OutCubic);

                // 【动画2：变透明】获取 Image 组件并直接使用 DOFade
                Image bgImage = backgroundPortrait.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.DOFade(0.4f, 0.5f); // 0.4f 代表降到 40% 的透明度，0.5f 是耗时
                }
            }
            else
            {
                Debug.LogWarning("未在当前场景实例中找到名为 SuspectBackground 的节点！");
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
    }
}