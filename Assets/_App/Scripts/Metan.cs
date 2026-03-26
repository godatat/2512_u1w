using UnityEngine;
using UnityEngine.UI;

public class Metan : Character
{
    public enum Facial
    {
        Normal,
        Miss,
        Happy,
        OrderNormal,
        OrderAngry,
        OrderHappy,
    }

    [SerializeField] private Image _image;
    [SerializeField] private Sprite[] facialSprites;

    public Facial facial = Facial.Normal;

    public void ChangeFacial(Facial facial)
    {
        _image.sprite = facialSprites[(int)facial];
    }
}

