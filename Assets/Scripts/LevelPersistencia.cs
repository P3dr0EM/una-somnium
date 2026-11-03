using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPersistencia : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string levelName = "area_externa";
    void Start()
    {
        if (!SceneManager.GetSceneByName(levelName).isLoaded)
        {
            SceneManager.LoadSceneAsync(
                levelName,
                LoadSceneMode.Additive
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
