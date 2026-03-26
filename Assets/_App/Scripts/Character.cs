using UnityEngine;
using System.Collections;
using DG.Tweening;

public class Character : MonoBehaviour
{
    public float amplitude = 0.05f;
    public float speed = 1f;
    public float shakeAmount = 10f;
    public float shakeDuration = 0.3f;
    public GameManager gameManager;
    public float maxSpeedMultiplier = 3f;
    public float maxRotationSpeed = 360f; // 最大回転速度（度/秒）
    public GameObject[] cloneObjects; // 分身用のGameObject配列
    public float cloneSpawnInterval = 0.2f; // 分身を1体ずつ表示する間隔
    public float bounceSpeed = 200f; // 跳ね返り移動の速度

    protected Vector2 baseAnchoredPos;
    protected RectTransform rectTransform;
    protected float timeOffset = 0f;
    private bool isLastTwoQuestions = false;
    
    // 跳ね返り移動用の変数
    private Vector2 bounceVelocity = Vector2.zero;
    private RectTransform canvasRectTransform;
    private Vector2 characterSize;
    private bool isBouncingMode = false;

    protected virtual void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        baseAnchoredPos = rectTransform.anchoredPosition;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        
        // キャンバスのRectTransformを取得
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }
        
        // キャラクターのサイズを取得
        characterSize = rectTransform.sizeDelta;
        
        // 初期のランダムな方向を設定
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        bounceVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized * bounceSpeed;
    }

    protected virtual void Update()
    {
        // ラスト2問かどうかを判定
        isLastTwoQuestions = gameManager != null && gameManager.RemainingQuestions <= 2;

        // 通常の上下動（常に適用）
        float ageAgeDoRatio = gameManager != null ? gameManager.AgeAgeDo / 100f : 0f;
        float currentSpeed = speed * (1f + ageAgeDoRatio * (maxSpeedMultiplier - 1f));
        float y = Mathf.Sin(Time.time * currentSpeed + timeOffset) * amplitude;
        Vector2 verticalOffset = new Vector2(0f, y);
        
        // 70%以上で跳ね返りモード
        bool shouldBounce = gameManager != null && gameManager.AgeAgeDo >= 80f;
        
        if (shouldBounce)
        {
            if (!isBouncingMode)
            {
                // 跳ね返りモードに切り替え
                isBouncingMode = true;
                // ランダムな方向を設定
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                bounceVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized * bounceSpeed;
            }
            
            // 跳ね返り移動（縦揺れに追加）
            Vector2 bounceOffset = UpdateBounceMovement();
            rectTransform.anchoredPosition = baseAnchoredPos + verticalOffset + bounceOffset;
        }
        else
        {
            if (isBouncingMode)
            {
                // 通常モードに戻す
                isBouncingMode = false;
            }
            
            // 通常の上下動のみ
            rectTransform.anchoredPosition = baseAnchoredPos + verticalOffset;
        }

        // ラスト2問でない場合のみ回転
        if (gameManager != null && gameManager.AgeAgeDo > 50f)
        {
            float rotationRatio = (gameManager.AgeAgeDo - 50f) / 50f; // 50-100の範囲を0-1に正規化
            float rotationSpeed = rotationRatio * maxRotationSpeed;
            rectTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
        else
        {
            rectTransform.rotation = Quaternion.identity;
        }
    }
    
    private Vector2 UpdateBounceMovement()
    {
        if (canvasRectTransform == null) return Vector2.zero;
        
        // キャンバスの境界を取得
        float canvasWidth = canvasRectTransform.rect.width;
        float canvasHeight = canvasRectTransform.rect.height;
        float halfWidth = canvasWidth * 0.5f;
        float halfHeight = canvasHeight * 0.5f;
        
        // ベース位置からの相対位置を取得
        Vector2 currentBouncePos = rectTransform.anchoredPosition - baseAnchoredPos;
        float halfCharWidth = characterSize.x * 0.5f;
        float halfCharHeight = characterSize.y * 0.5f;
        
        // 移動（baseAnchoredPosからの相対位置）
        Vector2 newBouncePos = currentBouncePos + bounceVelocity * Time.deltaTime;
        
        // 左右の境界チェック（baseAnchoredPosを基準とした相対位置）
        if (newBouncePos.x + baseAnchoredPos.x - halfCharWidth < -halfWidth || 
            newBouncePos.x + baseAnchoredPos.x + halfCharWidth > halfWidth)
        {
            bounceVelocity.x = -bounceVelocity.x;
            // 境界内に戻す
            float clampedX = Mathf.Clamp(newBouncePos.x + baseAnchoredPos.x, -halfWidth + halfCharWidth, halfWidth - halfCharWidth);
            newBouncePos.x = clampedX - baseAnchoredPos.x;
        }
        
        // 上下の境界チェック（baseAnchoredPosを基準とした相対位置）
        if (newBouncePos.y + baseAnchoredPos.y - halfCharHeight < -halfHeight || 
            newBouncePos.y + baseAnchoredPos.y + halfCharHeight > halfHeight)
        {
            bounceVelocity.y = -bounceVelocity.y;
            // 境界内に戻す
            float clampedY = Mathf.Clamp(newBouncePos.y + baseAnchoredPos.y, -halfHeight + halfCharHeight, halfHeight - halfCharHeight);
            newBouncePos.y = clampedY - baseAnchoredPos.y;
        }
        
        return newBouncePos;
    }

    public void ShowClones()
    {
        StartCoroutine(ShowClonesCoroutine());
    }

    IEnumerator ShowClonesCoroutine()
    {
        if (cloneObjects != null)
        {
            foreach (var clone in cloneObjects)
            {
                if (clone != null)
                {
                    clone.SetActive(true);
                    yield return new WaitForSeconds(cloneSpawnInterval);
                }
            }
        }
    }

    public void HideClones()
    {
        if (cloneObjects != null)
        {
            foreach (var clone in cloneObjects)
            {
                if (clone != null)
                {
                    clone.SetActive(false);
                }
            }
        }
    }

    public void Shake()
    {
        rectTransform.DOKill();
        rectTransform.DOShakeAnchorPos(shakeDuration, shakeAmount, 10, 90f, false, true);
    }
}
