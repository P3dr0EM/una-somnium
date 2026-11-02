using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePartLoader : MonoBehaviour
{
    private bool shouldLoad = false;
    private bool isLoaded = false;
    public String levelName = "";
    void Start()
    {
        if (SceneManager.sceneCount <= 0) return;
        for (int i = 0; i < SceneManager.sceneCount; i++) 
        {
            if (SceneManager.GetSceneAt(i).name == levelName) isLoaded = true;
        }
    }

    void Update()
    {
        TriggerCheck();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) shouldLoad = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) shouldLoad = false;
    }

    void TriggerCheck()
    {
        if (shouldLoad) LoadScene();
        else UnloadScene();
    }

    void LoadScene()
    {
        if (isLoaded) return;
        SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
        isLoaded = true;
    }

    void UnloadScene()
    {
        if (!isLoaded) return;
        SceneManager.UnloadSceneAsync(levelName);
        isLoaded = false;
    }
}
