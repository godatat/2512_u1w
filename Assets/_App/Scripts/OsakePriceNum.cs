using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class OsakePriceNum : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI text;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private float slideDistance = 50f; // スライドする距離

    private void Start()
    {
        canvasGroup.alpha = 0;
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    public void Show(int price)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                baseAnchoredPosition = rectTransform.anchoredPosition;
            }
        }
        
        canvasGroup.alpha = 0;
        text.text = $"+{price.ToString("N0")}円";
        
        // 下からスライドインする位置に設定
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(0f, -slideDistance);
        }
        
        // フェードと同時にスライドイン
        canvasGroup.DOFade(1, 0.5f);
        if (rectTransform != null)
        {
            rectTransform.DOAnchorPosY(baseAnchoredPosition.y, 0.5f).SetEase(Ease.OutQuad);
        }

        // 2秒後に非表示
        Invoke("Hide", 2f);
    }

    public void Hide()
    {
        canvasGroup.DOFade(0, 0.5f);
    }
}
