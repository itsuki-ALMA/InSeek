using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CriarCenaSerpente
{
    private const string PastaCenas = "Assets/SerpenteDaMasmorra/Scenes";
    private const string CaminhoCena = PastaCenas + "/SerpenteDaMasmorra.unity";

    [MenuItem("Tools/Serpente da Masmorra/Criar cena jogável")]
    public static void Criar()
    {
        if (File.Exists(CaminhoCena) && !EditorUtility.DisplayDialog(
            "Cena já existe",
            "A cena jogável já foi criada. Deseja substituí-la?",
            "Substituir",
            "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Directory.CreateDirectory(PastaCenas);
        AssetDatabase.Refresh();
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject gerenciador = new GameObject("Gerenciador da Serpente");
        gerenciador.AddComponent<SerpenteDaMasmorra>();
        EditorSceneManager.SaveScene(cena, CaminhoCena);

        EditorBuildSettingsScene[] atuais = EditorBuildSettings.scenes;
        bool encontrada = false;
        for (int i = 0; i < atuais.Length; i++)
        {
            if (atuais[i].path == CaminhoCena)
            {
                atuais[i].enabled = true;
                encontrada = true;
                break;
            }
        }
        if (!encontrada)
        {
            EditorBuildSettingsScene[] atualizadas = new EditorBuildSettingsScene[atuais.Length + 1];
            for (int i = 0; i < atuais.Length; i++) atualizadas[i] = atuais[i];
            atualizadas[atualizadas.Length - 1] = new EditorBuildSettingsScene(CaminhoCena, true);
            EditorBuildSettings.scenes = atualizadas;
        }

        AssetDatabase.Refresh();
        Debug.Log("Cena criada em " + CaminhoCena + ". Pressione Play para iniciar.");
    }
}
