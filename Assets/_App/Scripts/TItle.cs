using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TItle : MonoBehaviour
{
    public AudioSource sePlayer;
    public AudioClip clickSE;

    public void StartGame()
    {
        sePlayer.PlayOneShot(clickSE);

        // 若干ディレイ入れる
        StartCoroutine(DelayCoroutine());
    }

    IEnumerator DelayCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        SceneTransition.LoadScene("SampleScene");
    }
}
