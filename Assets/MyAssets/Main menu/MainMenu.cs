using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen_gameObject;
    [SerializeField] private string gameSceneName;

    private LoadingScreen loadingScreen;



    // ----- START -----
    private void Start()
    {
        loadingScreen = loadingScreen_gameObject.GetComponent<LoadingScreen>();
    }



    // ----- BUTTON EVENTS -----
    public void ButtonStartGame_OnClick()
    {
        // Handle music


        StartCoroutine(LoadSceneAsync(gameSceneName));
    }
    public void ButtonQuitGame_OnClick()
    {
        // Handle music


        // Exit application
        Application.Quit();
    }



    // ----- SCENE MANAGEMENT ----
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        loadingScreen_gameObject.SetActive(true);

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            loadingScreen.SetProgressBar(progress);

            yield return null;
        }

        loadingScreen_gameObject.SetActive(false);
    }
}
