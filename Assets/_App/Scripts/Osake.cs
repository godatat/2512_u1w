using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Osake : MonoBehaviour
{
    public enum OsakeType
    {
        _ノンアル,
        _安シャン,
        _シャンディガフ,
        _シャンパン,
        _シャンパンタワー,
    }

    public CanvasGroup canvasGroup;
    public Image image;
    public Sprite[] sprites;

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

    public void Show(OsakeType osakeType)
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
        image.sprite = sprites[(int)osakeType];
        
        // 上からスライドインする位置に設定
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(0f, slideDistance);
        }
        
        // フェードと同時にスライドイン
        canvasGroup.DOFade(1, 0.5f);
        if (rectTransform != null)
        {
            rectTransform.DOAnchorPosY(baseAnchoredPosition.y, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    public void Hide()
    {
        canvasGroup.DOFade(0, 0.5f);
    }
}
