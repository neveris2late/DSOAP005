using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SuperHintController : MonoBehaviour
{
    public static SuperHintController Instance;

    [Header("UI References")]
    public RectTransform canvasRect;
    public RectTransform hintBox;
    public TextMeshProUGUI hintTextUI;

    [Header("Line Settings")]
    public Image lineSegmentV;       
    public Image lineSegmentH;       
    public Color customLineColor = Color.cyan; 
    public float lineWidth = 4f;     
    public float blackoutPaddingFactor = 1.2f; 

    [Header("Close Button Settings")]
    public Button closeBtn;
    public float closeBtnDelay = 2.0f;

    [Header("Prefabs & Settings")]
    public GameObject framePrefab;   
    public float mainShrinkDuration = 1.0f;
    public int echoFrameCount = 3;
    public Color[] frameColors = new Color[] { Color.cyan, Color.yellow, Color.magenta };

    private List<RectTransform> activeFrames = new List<RectTransform>();
    private Coroutine _showCloseBtnCoroutine;
    
    private Image[] blackoutMaskPanels = new Image[4]; 
    private RectTransform dummyHole; 
    private Canvas _parentCanvas;

    void Awake()
    {
        Instance = this;
        _parentCanvas = canvasRect.GetComponentInParent<Canvas>();

        if (hintBox) hintBox.gameObject.SetActive(false);
        if (lineSegmentV) lineSegmentV.gameObject.SetActive(false);
        if (lineSegmentH) lineSegmentH.gameObject.SetActive(false);

        if (lineSegmentV) lineSegmentV.color = customLineColor;
        if (lineSegmentH) lineSegmentH.color = customLineColor;

        if (closeBtn != null)
        {
            closeBtn.gameObject.SetActive(false);
            closeBtn.onClick.AddListener(HideSuperHint);
        }

        InitializeDynamicBlackout();
    }
    
    private void InitializeDynamicBlackout()
    {
        GameObject dummyGo = new GameObject("DummyHole_Internal");
        dummyGo.transform.SetParent(canvasRect, false);
        dummyHole = dummyGo.AddComponent<RectTransform>();
        dummyHole.anchorMin = new Vector2(0.5f, 0.5f);
        dummyHole.anchorMax = new Vector2(0.5f, 0.5f);
        dummyHole.pivot = new Vector2(0.5f, 0.5f);
        dummyGo.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            GameObject panelGo = new GameObject($"BlackoutPanel_{i}");
            panelGo.transform.SetParent(canvasRect, false);
            Image img = panelGo.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.85f); 
            img.raycastTarget = true; 
            blackoutMaskPanels[i] = img;
            
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            panelGo.SetActive(false);
        }

        blackoutMaskPanels[0].rectTransform.pivot = new Vector2(0.5f, 0f);   
        blackoutMaskPanels[1].rectTransform.pivot = new Vector2(0.5f, 1f);   
        blackoutMaskPanels[2].rectTransform.pivot = new Vector2(1f, 0.5f);   
        blackoutMaskPanels[3].rectTransform.pivot = new Vector2(0f, 0.5f);   
    }

    public void ShowSuperHint(RectTransform targetUI, string hintMessage)
    {
        HideSuperHint(); 
        hintTextUI.text = hintMessage;
        StartCoroutine(PlaySuperHintEffectRoutine(targetUI));
    }

    private IEnumerator PlaySuperHintEffectRoutine(RectTransform targetUI)
    {
        if (targetUI == null) yield break;

        // ==========================================
        // 【最强坐标映射】：将目标通过屏幕空间完美映射到当前 Canvas
        // ==========================================
        Camera cam = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera;
        Vector3[] corners = new Vector3[4];
        targetUI.GetWorldCorners(corners);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            // 1. 世界坐标 -> 屏幕像素坐标
            Vector2 screenP = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            // 2. 屏幕像素坐标 -> Canvas完美局部坐标
            Vector2 localP;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenP, cam, out localP);
            
            minX = Mathf.Min(minX, localP.x);
            minY = Mathf.Min(minY, localP.y);
            maxX = Mathf.Max(maxX, localP.x);
            maxY = Mathf.Max(maxY, localP.y);
        }

        Vector3 targetLocalPos = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector2 targetHoleSize = new Vector2(maxX - minX, maxY - minY) * blackoutPaddingFactor;

        dummyHole.gameObject.SetActive(true);
        foreach (var p in blackoutMaskPanels) 
        {
            p.gameObject.SetActive(true);
            p.rectTransform.SetAsLastSibling(); 
        }

        dummyHole.localPosition = targetLocalPos;
        dummyHole.sizeDelta = new Vector2(canvasRect.rect.width * 2f, canvasRect.rect.height * 2f);

        Tween mainTween = dummyHole.DOSizeDelta(targetHoleSize, mainShrinkDuration).SetEase(Ease.OutQuint)
            .OnUpdate(UpdateBlackoutPanels);

        for (int i = 0; i < echoFrameCount; i++)
        {
            yield return new WaitForSeconds(mainShrinkDuration / (echoFrameCount + 1.5f));

            Color randomColor = frameColors[Random.Range(0, frameColors.Length)];
            randomColor.a = 1f; 
            RectTransform echoFrame = CreateFrame(randomColor);

            echoFrame.sizeDelta = new Vector2(canvasRect.rect.width * 3f, canvasRect.rect.height * 3f);
            echoFrame.localPosition = targetLocalPos; 

            echoFrame.DOSizeDelta(targetHoleSize, mainShrinkDuration * 1.2f).SetEase(Ease.OutQuad)
                .OnUpdate(() =>
                {
                    if (dummyHole && echoFrame.sizeDelta.x <= dummyHole.sizeDelta.x)
                    {
                        echoFrame.DOKill();
                        Destroy(echoFrame.gameObject);
                    }
                });
        }

        yield return mainTween.WaitForCompletion();

        hintBox.gameObject.SetActive(true);
        hintBox.SetAsLastSibling(); 
        hintBox.localPosition = Vector3.zero; 

        CanvasGroup boxGroup = hintBox.GetComponent<CanvasGroup>();
        if (boxGroup == null) boxGroup = hintBox.gameObject.AddComponent<CanvasGroup>();
        boxGroup.alpha = 0;
        boxGroup.DOFade(1, 0.3f);
        hintBox.DOScale(Vector3.one, 0.3f).From(Vector3.zero).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.3f);

        DrawBentLine(hintBox);

        if (closeBtn != null) _showCloseBtnCoroutine = StartCoroutine(ShowCloseButtonRoutine());
    }

    private void UpdateBlackoutPanels()
    {
        Vector3 center = dummyHole.localPosition;
        Vector2 size = dummyHole.sizeDelta;
        float bigExtents = 15000f; 

        blackoutMaskPanels[0].rectTransform.localPosition = new Vector3(center.x, center.y + size.y / 2f, 0);
        blackoutMaskPanels[0].rectTransform.sizeDelta = new Vector2(bigExtents, bigExtents);

        blackoutMaskPanels[1].rectTransform.localPosition = new Vector3(center.x, center.y - size.y / 2f, 0);
        blackoutMaskPanels[1].rectTransform.sizeDelta = new Vector2(bigExtents, bigExtents);

        blackoutMaskPanels[2].rectTransform.localPosition = new Vector3(center.x - size.x / 2f, center.y, 0);
        blackoutMaskPanels[2].rectTransform.sizeDelta = new Vector2(bigExtents, size.y);

        blackoutMaskPanels[3].rectTransform.localPosition = new Vector3(center.x + size.x / 2f, center.y, 0);
        blackoutMaskPanels[3].rectTransform.sizeDelta = new Vector2(bigExtents, size.y);
    }

    private RectTransform CreateFrame(Color color)
    {
        GameObject go = Instantiate(framePrefab, canvasRect);
        Image img = go.GetComponent<Image>();
        if (img != null) { img.color = color; img.type = Image.Type.Sliced; }
        
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetAsLastSibling(); 
        activeFrames.Add(rt);
        return rt;
    }

    private void DrawBentLine(RectTransform boxRect)
    {
        if (lineSegmentV == null || lineSegmentH == null) return;

        lineSegmentV.gameObject.SetActive(true); lineSegmentH.gameObject.SetActive(true);
        lineSegmentV.transform.SetAsLastSibling(); lineSegmentH.transform.SetAsLastSibling();

        Vector3 startLocal = dummyHole.localPosition + new Vector3(0, dummyHole.sizeDelta.y / 2f, 0);

        Camera cam = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera;
        Vector3[] boxCorners = new Vector3[4];
        boxRect.GetWorldCorners(boxCorners);
        Vector3 boxBottomWorld = (boxCorners[0] + boxCorners[3]) / 2f;
        
        Vector2 screenP = RectTransformUtility.WorldToScreenPoint(cam, boxBottomWorld);
        Vector2 endLocal2D;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenP, cam, out endLocal2D);
        Vector3 endLocal = new Vector3(endLocal2D.x, endLocal2D.y, 0f);

        Vector3 cornerLocal = new Vector3(startLocal.x, endLocal.y, 0);

        bool isTargetBelow = startLocal.y < cornerLocal.y;
        lineSegmentV.rectTransform.pivot = new Vector2(0.5f, isTargetBelow ? 0f : 1f);
        lineSegmentV.rectTransform.localPosition = startLocal; 
        lineSegmentV.rectTransform.sizeDelta = new Vector2(lineWidth, 0);

        float vDistance = Mathf.Abs(startLocal.y - cornerLocal.y);
        lineSegmentV.rectTransform.DOSizeDelta(new Vector2(lineWidth, vDistance), 0.2f).SetEase(Ease.OutQuad);

        bool isEndRight = endLocal.x > cornerLocal.x;
        lineSegmentH.rectTransform.pivot = new Vector2(isEndRight ? 0f : 1f, 0.5f);
        lineSegmentH.rectTransform.localPosition = cornerLocal; 
        lineSegmentH.rectTransform.sizeDelta = new Vector2(0, lineWidth);

        float hDistance = Mathf.Abs(cornerLocal.x - endLocal.x);
        lineSegmentH.rectTransform.DOSizeDelta(new Vector2(hDistance, lineWidth), 0.2f).SetEase(Ease.OutQuad).SetDelay(0.15f);
    }

    private IEnumerator ShowCloseButtonRoutine()
    {
        yield return new WaitForSeconds(closeBtnDelay);
        if (closeBtn == null) yield break;
        closeBtn.gameObject.SetActive(true);
        CanvasGroup btnGroup = closeBtn.GetComponent<CanvasGroup>();
        if (btnGroup == null) btnGroup = closeBtn.gameObject.AddComponent<CanvasGroup>();
        btnGroup.alpha = 0;
        btnGroup.DOFade(1f, 0.5f);
    }

    public void HideSuperHint()
    {
        if (_showCloseBtnCoroutine != null) StopCoroutine(_showCloseBtnCoroutine);

        if (hintBox) hintBox.gameObject.SetActive(false);
        if (lineSegmentV) lineSegmentV.gameObject.SetActive(false);
        if (lineSegmentH) lineSegmentH.gameObject.SetActive(false);
        if (dummyHole) dummyHole.gameObject.SetActive(false);
        
        foreach (var p in blackoutMaskPanels) if (p != null) p.gameObject.SetActive(false);

        if (hintBox) hintBox.DOKill();
        if (lineSegmentV) lineSegmentV.rectTransform.DOKill();
        if (lineSegmentH) lineSegmentH.rectTransform.DOKill();
        if (dummyHole) dummyHole.DOKill();

        foreach (var frame in activeFrames)
        {
            if (frame != null) { frame.DOKill(); Destroy(frame.gameObject); }
        }
        activeFrames.Clear();
    }
}