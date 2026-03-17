using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // 引入 DOTween 命名空间

public class JudgmentController : MonoBehaviour
{
    [Header("UI 容器引用")]
    public GameObject phase1Container; 
    public GameObject phase2Container; 
    public GameObject noSpinalTestWarningPanel; 
    public TextMeshProUGUI spinalTestCountText; 

    [Header("动画控制器引用 (需手动挂载)")]
    [Tooltip("挂载在 phase1Container 上的 ParentAnimController")]
    public ParentAnimController phase1Anim;
    [Tooltip("挂载在 phase2Container 上的 ParentAnimController")]
    public ParentAnimController phase2Anim;
    [Tooltip("挂载在 warningPanel 上的 ParentAnimController")]
    public ParentAnimController warningAnim;
    
    [Tooltip("退场动画预估耗时，需与 ElementAnimController 匹配")]
    public float exitWaitTime = 0.3f; 

    [Header("卡片按钮引用 (Phase 1)")]
    public Button btnInnocent;      
    public Button btnArrestSpy;     
    public Button btnAndroid;       

    [Header("卡片按钮引用 (Phase 2)")]
    public Button btnExecuteAndroid; 
    public Button btnSpinalTest;     

    [Header("警告面板引用")]
    public Button btnCloseWarning;   

    [Header("系统配置")]
    public int availableSpinalTests = 1; 

    private RuntimeSuspect currentSuspect;

    private void Start()
    {
        BindAllButtons();
        ForceCloseAllPanels(); // 初始化时强制无动画关闭
    }

    private void BindAllButtons()
    {
        if (btnInnocent != null) btnInnocent.onClick.AddListener(SelectInnocent);
        if (btnArrestSpy != null) btnArrestSpy.onClick.AddListener(SelectArrestSpy);
        if (btnAndroid != null) btnAndroid.onClick.AddListener(SelectAndroid);

        if (btnExecuteAndroid != null) btnExecuteAndroid.onClick.AddListener(SelectExecuteAndroid);
        if (btnSpinalTest != null) btnSpinalTest.onClick.AddListener(SelectSpinalTest);

        if (btnCloseWarning != null) btnCloseWarning.onClick.AddListener(CloseWarningPanel);
    }

    // ================= 核心流程 =================

    public void OpenJudgmentPanel(RuntimeSuspect suspect)
    {
        currentSuspect = suspect;
        ForceCloseAllPanels();
        
        phase1Container.SetActive(true);
        if (phase1Anim != null) phase1Anim.PlayEnter(); // 播放入场瀑布流
        
        if (spinalTestCountText != null)
        {
            spinalTestCountText.gameObject.SetActive(true);
            UpdateSpinalTestText();
        }
    }

    private void SelectInnocent() => SubmitJudgment(PlayerJudgment.Innocent);
    private void SelectArrestSpy() => SubmitJudgment(PlayerJudgment.ArrestSpy);

    private void SelectAndroid()
    {
        // 1. 播放第一阶段退场动画
        if (phase1Anim != null) phase1Anim.PlayExit();
        
        // 2. 等待动画结束，切换到第二阶段
        DOVirtual.DelayedCall(exitWaitTime, () =>
        {
            phase1Container.SetActive(false);
            phase2Container.SetActive(true);
            if (phase2Anim != null) phase2Anim.PlayEnter(); // 第二阶段入场
        });
    }

    private void SelectExecuteAndroid() => SubmitJudgment(PlayerJudgment.ExecuteAndroid);

    private void SelectSpinalTest()
    {
        if (availableSpinalTests > 0)
        {
            availableSpinalTests--;
            UpdateSpinalTestText(); 
            SubmitJudgment(PlayerJudgment.TestAndroid);
        }
        else
        {
            // 次数用尽，动画切换至警告面板
            if (phase2Anim != null) phase2Anim.PlayExit();
            DOVirtual.DelayedCall(exitWaitTime, () =>
            {
                phase2Container.SetActive(false);
                noSpinalTestWarningPanel.SetActive(true);
                if (warningAnim != null) warningAnim.PlayEnter();
            });
        }
    }
    
    private void CloseWarningPanel()
    {
        // 从警告面板退回第二阶段
        if (warningAnim != null) warningAnim.PlayExit();
        DOVirtual.DelayedCall(exitWaitTime, () =>
        {
            noSpinalTestWarningPanel.SetActive(false);
            phase2Container.SetActive(true);
            if (phase2Anim != null) phase2Anim.PlayEnter();
        });
    }

    private void SubmitJudgment(PlayerJudgment judgment)
    {
        if (currentSuspect != null)
        {
            currentSuspect.finalJudgment = judgment;
            Debug.Log($"[判定完成] 嫌疑人: {currentSuspect.BaseData.suspectName}, 判定结果: {judgment}");
        }

        // 判定哪个面板正处于激活状态，播放其退场动画
        if (phase1Container.activeSelf && phase1Anim != null) phase1Anim.PlayExit();
        if (phase2Container.activeSelf && phase2Anim != null) phase2Anim.PlayExit();
        
        // 延迟后彻底关闭
        DOVirtual.DelayedCall(exitWaitTime, ForceCloseAllPanels);
    }

    private void ForceCloseAllPanels()
    {
        if (phase1Container != null) phase1Container.SetActive(false);
        if (phase2Container != null) phase2Container.SetActive(false);
        if (noSpinalTestWarningPanel != null) noSpinalTestWarningPanel.SetActive(false);
        if (spinalTestCountText != null) spinalTestCountText.gameObject.SetActive(false);
    }

    private void UpdateSpinalTestText()
    {
        if (spinalTestCountText != null)
        {
            spinalTestCountText.text = $"警署允许您申请的脊髓测试还剩 {availableSpinalTests} / 1";
        }
    }
}