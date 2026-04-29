using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadCurrentScene : MonoBehaviour
{
    [SerializeField] private int nextSceneBuildIndex;

    public void NextScene() 
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (SceneManager.sceneCountInBuildSettings - 1 <= currentIndex) 
        {
            SceneManager.LoadScene(0);
        }
        else 
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void NextSceneIndex() 
    {
        SceneManager.LoadScene(nextSceneBuildIndex);
    }
    public void ReloadScene() 
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
