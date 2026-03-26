using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AgeGauge : MonoBehaviour
{
    [SerializeField] private RectTransform _gaugeRectTransform;
    [SerializeField] private float _maxWidth = 100f;
    [SerializeField] private float _animationDuration = 0.3f;

    private float _baseWidth = 0f;
    private Image _gaugeImage;
    private float _currentValue = 0f;
    private bool _isRainbowMode = false;
    private float _rainbowHue = 0f;
    private float _rainbowSpeed = 1f; // レインボーの色変化速度

    void Start()
    {
        _baseWidth = GetComponent<RectTransform>().sizeDelta.x;
        _gaugeImage = _gaugeRectTransform.GetComponent<Image>();
        if (_gaugeImage == null)
        {
            Debug.LogError("AgeGauge: Image component not found on _gaugeRectTransform");
        }
    }

    void Update()
    {
        // 70%以上でレインボー効果
        if (_isRainbowMode && _gaugeImage != null)
        {
            _rainbowHue += _rainbowSpeed * Time.deltaTime;
            if (_rainbowHue >= 1f)
            {
                _rainbowHue -= 1f;
            }
            
            Color rainbowColor = Color.HSVToRGB(_rainbowHue, 1f, 1f);
            _gaugeImage.color = rainbowColor;
        }
    }

    public void SetValue(float value)
    {
        value = Mathf.Clamp01(value);
        _currentValue = value;
        Debug.Log("value: " + value);
        float targetWidth = _baseWidth * value;
        _gaugeRectTransform.DOKill();
        _gaugeRectTransform.DOSizeDelta(new Vector2(targetWidth, _gaugeRectTransform.sizeDelta.y), _animationDuration).SetEase(Ease.OutQuad);
        
        // 色を更新
        UpdateColor(value);
    }

    private void UpdateColor(float value)
    {
        if (_gaugeImage == null) return;

        if (value <= 0.2f)
        {
            // 20%以下：赤
            _isRainbowMode = false;
            _gaugeImage.color = Color.red;
        }
        else if (value < 0.7f)
        {
            // 20%以上70%未満：00FFF9（シアン系）
            _isRainbowMode = false;
            Color cyanColor = new Color(0f / 255f, 255f / 255f, 249f / 255f, 1f);
            _gaugeImage.color = cyanColor;
        }
        else
        {
            // 70%以上：レインボー
            _isRainbowMode = true;
            // レインボーモードではUpdate()で色が更新される
        }
    }
}
