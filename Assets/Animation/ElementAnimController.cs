using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))] // 自动添加必备组件，防止漏挂报错
public class ElementAnimController : MonoBehaviour
{
    [Header("入场动画 (Enter)")]
    public float enterMoveDuration = 0.4f;
    public float enterFadeDuration = 0.3f;
    public Vector2 enterOffset = new Vector2(-100f, 0f);
    public Ease enterMoveEase = Ease.OutExpo;

    [Header("赛博闪烁特效 (Flicker)")]
    public int flickerMin = 2;
    public int flickerMax = 4;
    public float flickerSingleDuration = 0.05f;
    public float flickerMinAlpha = 0.5f;

    [Header("退场动画 (Exit)")]
    public float exitMoveDuration = 0.2f;
    public float exitFadeDuration = 0.2f;
    public Ease exitMoveEase = Ease.InExpo;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 originalPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 记录物体在拼UI时的初始正确位置
        originalPos = rect.anchoredPosition;
    }

    public void PlayEnter(float delay)
    {
        // 瞬间移动到偏移位置，并完全透明
        rect.anchoredPosition = originalPos + enterOffset;
        canvasGroup.alpha = 0;

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay); // 瀑布流延迟

        // 位移和渐显同时进行
        seq.Join(rect.DOAnchorPos(originalPos, enterMoveDuration).SetEase(enterMoveEase));
        seq.Join(canvasGroup.DOFade(1f, enterFadeDuration));

        // 动画结束后触发闪烁
        seq.AppendCallback(PlayFlicker);
    }

    void PlayFlicker()
    {
        int times = Random.Range(flickerMin, flickerMax + 1);

        for (int i = 0; i < times; i++)
        {
            canvasGroup.DOFade(
                Random.Range(flickerMinAlpha, 1f),
                flickerSingleDuration
            ).SetLoops(2, LoopType.Yoyo);
        }
    }

    public void PlayExit(float delay)
    {
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay); // 瀑布流延迟

        // 带着偏移量飞出，并同时淡出
        seq.Join(rect.DOAnchorPos(originalPos + enterOffset, exitMoveDuration).SetEase(exitMoveEase));
        seq.Join(canvasGroup.DOFade(0f, exitFadeDuration));
    }
}