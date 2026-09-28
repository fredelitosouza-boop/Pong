#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor helper — configura o background e a bola do Pong com os sprites dos gatos.
/// Menu: 
///   - Pong → Configurar Background
///   - Pong → Configurar Bola (Gato)
/// </summary>
public static class BackgroundSetupEditor
{
    private const string BG_IMAGE_PATH = "Assets/WhatsApp Image 2026-09-23 at 12.32.20.jpeg";
    private const string BG_NAME = "Background";

    private const string BOLA_SPRITE_PATH = "Assets/Sprites/BolaGato.png";
    private const string BOLA_NAME = "Bola";

    [MenuItem("Pong/Configurar Background")]
    public static void ConfigurarBackground()
    {
        // 1. Carregar o sprite da imagem
        Sprite sprite = CarregarSprite(BG_IMAGE_PATH);
        if (sprite == null)
        {
            Debug.LogError($"[BackgroundSetup] Não foi possível encontrar o Sprite em: {BG_IMAGE_PATH}");
            return;
        }

        // 2. Localizar ou criar o GameObject Background
        GameObject bgGO = GameObject.Find(BG_NAME);
        if (bgGO == null)
        {
            bgGO = new GameObject(BG_NAME);
            Undo.RegisterCreatedObjectUndo(bgGO, "Criar Background");
        }
        else
        {
            Undo.RecordObject(bgGO, "Configurar Background");
        }

        // 3. Configurar Transform
        Undo.RecordObject(bgGO.transform, "Posicionar Background");
        bgGO.transform.position = Vector3.zero;
        bgGO.transform.rotation = Quaternion.identity;

        // 4. Configurar SpriteRenderer
        SpriteRenderer sr = bgGO.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = Undo.AddComponent<SpriteRenderer>(bgGO);
        }
        else
        {
            Undo.RecordObject(sr, "Configurar SpriteRenderer");
        }

        sr.sprite = sprite;
        sr.sortingOrder = -10; // Fica atrás de todos os elementos da cena (raquetes, bola, placar)

        // 5. Garantir componente BackgroundFitter para adaptação automática a telas Android
        BackgroundFitter fitter = bgGO.GetComponent<BackgroundFitter>();
        if (fitter == null)
        {
            fitter = Undo.AddComponent<BackgroundFitter>(bgGO);
        }
        else
        {
            Undo.RecordObject(fitter, "Configurar BackgroundFitter");
        }

        fitter.modo = BackgroundFitter.ModoAjuste.Cover;
        fitter.margemSeguranca = 1.02f;
        fitter.seguirCamera = true;
        fitter.AjustarAgora();

        // 6. Marcar objeto como sujo e selecionar no hierarchy
        EditorUtility.SetDirty(bgGO);
        Selection.activeGameObject = bgGO;
        EditorGUIUtility.PingObject(bgGO);

        Debug.Log("[BackgroundSetup] ✅ Background com adaptação Android configurado com sucesso na cena!");
    }

    [MenuItem("Pong/Configurar Bola (Gato)")]
    public static void ConfigurarBolaGato()
    {
        // 1. Carregar o sprite circular da bola de gato
        Sprite sprite = CarregarSprite(BOLA_SPRITE_PATH);
        if (sprite == null)
        {
            Debug.LogError($"[BolaSetup] Sprite não encontrado em: {BOLA_SPRITE_PATH}");
            return;
        }

        // 2. Localizar a Bola na cena
        GameObject bolaGO = GameObject.Find(BOLA_NAME);
        if (bolaGO == null)
        {
            Debug.LogError($"[BolaSetup] GameObject '{BOLA_NAME}' não encontrado na cena!");
            return;
        }

        Undo.RecordObject(bolaGO, "Configurar Bola de Gato");

        // 3. Atualizar SpriteRenderer
        SpriteRenderer sr = bolaGO.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = Undo.AddComponent<SpriteRenderer>(bolaGO);
        }
        else
        {
            Undo.RecordObject(sr, "Configurar Sprite da Bola");
        }

        sr.sprite = sprite;
        sr.color = Color.white; // Branco para preservar as cores reais do gatinho
        sr.sortingOrder = 1;    // Fica acima do background e paredes

        // 4. Ajustar escala e colisor para ficar perfeitamente alinhado
        Undo.RecordObject(bolaGO.transform, "Ajustar Escala Bola");
        bolaGO.transform.localScale = Vector3.one; // Diâmetro de 1.0 unidade perfeitamente circular

        CircleCollider2D col = bolaGO.GetComponent<CircleCollider2D>();
        if (col != null)
        {
            Undo.RecordObject(col, "Ajustar Colisor");
            col.radius = 0.5f;
            col.offset = Vector2.zero;
        }

        EditorUtility.SetDirty(bolaGO);
        Selection.activeGameObject = bolaGO;
        EditorGUIUtility.PingObject(bolaGO);

        Debug.Log("[BolaSetup] 🐱 Bola de Gato configurada com sucesso na cena!");
    }

    private static Sprite CarregarSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is Sprite spriteAsset)
            {
                return spriteAsset;
            }
        }

        return null;
    }
}
#endif
