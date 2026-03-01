using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class FloatingClue : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string clueID { get; private set; }
    public bool isGood { get; private set; }

    public TextMeshProUGUI clueNameText;
    public Button closeButton; 
    public CanvasGroup canvasGroup; 

    [Header("物理漂浮设置")]
    public float floatSpeed = 80f; // 漂浮的初始推力
    private Rigidbody2D rb;

    private void Awake()
    {
        // 获取预制体上的刚体组件
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(string id, string text, bool goodAttr)
    {
        clueID = id;
        clueNameText.text = text;
        isGood = goodAttr;
        closeButton.gameObject.SetActive(false); 

        closeButton.onClick.AddListener(OnCloseClicked);
        StartFloating();
    }

    private void StartFloating()
    {
        // 随机在原位置附近生成一点偏移，防止所有线索叠在一起
        transform.localPosition = new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0);
        
        // 【修改点】：废弃 DOTween 漂浮，改为物理推力
        if (rb != null)
        {
            // 给一个随机的 2D 方向
            Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            rb.velocity = randomDir * floatSpeed; // 赋予初始速度，后续靠物理材质反弹
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        closeButton.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        closeButton.gameObject.SetActive(false);
    }

    private void OnCloseClicked()
    {
        if (rb != null) rb.simulated = false; // 停止物理演算，防止与后续动画冲突
        transform.DOKill();
        canvasGroup.DOFade(0, 0.3f);
        transform.DOScale(0.5f, 0.3f).OnComplete(() => 
        {
            ScoreController.Instance.RemoveClue(this);
        });
    }
}