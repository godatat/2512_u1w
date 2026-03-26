using UnityEngine;

public class OneMoreCall : MonoBehaviour
{
    public AudioClip[] oneMoreCallSE;
    public AudioSource sePlayer;

    public void PlayRandomSE()
    {
        int randomIndex = Random.Range(0, oneMoreCallSE.Length);
        sePlayer.PlayOneShot(oneMoreCallSE[randomIndex]);
    }
}
