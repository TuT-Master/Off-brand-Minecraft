using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image progressBar_fill;
    public void SetProgressBar(float value01)
    {
        progressBar_fill.fillAmount = value01;
    }
}
