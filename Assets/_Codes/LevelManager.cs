using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{

    public SheepCounter sheepcounter;

    [ReadOnly] public int totalScenes;
    private int currentScene;
    public float sceneLoadDelay=1f;
    private void Awake()
    {
        sheepcounter = GetComponent<SheepCounter>();
        sheepcounter.OnAllAnimalsEaten.AddListener(OnAllAnimalsEaten);
        
        totalScenes=SceneManager.sceneCount;
        currentScene=SceneManager.GetActiveScene().buildIndex;
    }

    void OnAllAnimalsEaten()
    {
        int nextSceneCount = currentScene++;
        currentScene%= totalScenes;

        StartCoroutine((LoadSceneWithDelay(currentScene)));
    }

        IEnumerator LoadSceneWithDelay(int scene)
        {
            yield return new WaitForSeconds(sceneLoadDelay);
            SceneManager.LoadScene(scene);
        }
    private void OnDestroy()
    {
        sheepcounter.OnAllAnimalsEaten.RemoveListener(OnAllAnimalsEaten);
    }

    public void GotfuckedUp()
    {
        StartCoroutine((LoadSceneWithDelay(SceneManager.GetActiveScene().buildIndex)));
    }
}
