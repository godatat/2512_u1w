using UnityEngine;
using TMPro;

public class ResultScene : MonoBehaviour
{
    public AudioSource sePlayer;
    public AudioClip clickSE;
    [SerializeField] private TextMeshProUGUI _resultText;

    void Start()
    {
        if (_resultText != null)
        {
            _resultText.text = $"入力時間: {GameResultData.TotalTime:F2}秒\n\n正確性: {GameResultData.Accuracy:F2}%\n\n稼いだお金: {GameResultData.TotalEarnedMoney:N0}円";
        }
    }

    public void OnClickReturnTitle()
    {
        sePlayer.PlayOneShot(clickSE);
        SceneTransition.LoadScene("TitleScene");
    }
}

