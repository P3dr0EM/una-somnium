using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPersistencia : MonoBehaviour
{
    // Sala carregada junto com a "persistencia" ao abrir o jogo.
    // 2026-11-15: "saguao" (o jogo começa no Saguão; antes era "area_externa").
    public string levelName = "saguao";

    void Start()
    {
        if (SceneManager.GetSceneByName(levelName).isLoaded) return;

        // Teste no editor: se outra cena já foi aberta junto (uma sala, a area_externa...), respeita e não carrega nada.
        // No jogo (build), a persistencia abre sozinha e carrega a sala inicial.
        if (SceneManager.sceneCount > 1) return;

        SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
    }
}
