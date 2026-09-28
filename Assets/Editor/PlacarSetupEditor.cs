#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Editor helper — cria automaticamente o Canvas de placar do Pong.
/// Menu: Pong → Configurar Placar UI
/// </summary>
public static class PlacarSetupEditor
{
    [MenuItem("Pong/Configurar Placar UI")]
    static void ConfigurarPlacar()
    {
        // ── 1. Canvas ─────────────────────────────────────────────────────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Criar Canvas");
        }

        // ── 2. Objeto Placar (PlacarController) ───────────────────────────────
        GameObject placarGO = GameObject.Find("Placar");
        if (placarGO == null)
        {
            placarGO = new GameObject("Placar");
            Undo.RegisterCreatedObjectUndo(placarGO, "Criar Placar");
        }

        PlacarController placarCtrl = placarGO.GetComponent<PlacarController>();
        if (placarCtrl == null)
            placarCtrl = Undo.AddComponent<PlacarController>(placarGO);

        // ── 3. Textos do Placar ───────────────────────────────────────────────
        TMP_Text textoJogador = CriarTexto(canvas.transform, "TextoPlacarJogador",
            "0",
            new Vector2(0f, 1f),        // anchor: canto superior esquerdo
            new Vector2(0f, 1f),
            new Vector2(120f, -60f));   // posição local

        TMP_Text textoIA = CriarTexto(canvas.transform, "TextoPlacarIA",
            "0",
            new Vector2(1f, 1f),        // anchor: canto superior direito
            new Vector2(1f, 1f),
            new Vector2(-120f, -60f));  // posição local

        // ── 4. Painel de Vitória ──────────────────────────────────────────────
        GameObject painelVitoria = GameObject.Find("PainelVitoria");
        TMP_Text textoVitoria = null;
        if (painelVitoria == null)
        {
            painelVitoria = new GameObject("PainelVitoria");
            painelVitoria.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(painelVitoria, "Criar PainelVitoria");

            RectTransform rt = painelVitoria.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.3f);
            rt.anchorMax = new Vector2(0.8f, 0.7f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = painelVitoria.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.8f);

            // Texto dentro do painel
            textoVitoria = CriarTexto(painelVitoria.transform, "TextoVitoria",
                "Vencedor!",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero);

            textoVitoria.alignment = TextAlignmentOptions.Center;
            textoVitoria.fontSize  = 48;
        }
        else
        {
            textoVitoria = painelVitoria.GetComponentInChildren<TMP_Text>();
        }

        painelVitoria.SetActive(false);

        // ── 5. Conectar referências no PlacarController ───────────────────────
        SerializedObject so = new SerializedObject(placarCtrl);
        so.FindProperty("textoJogador").objectReferenceValue  = textoJogador;
        so.FindProperty("textoIA").objectReferenceValue       = textoIA;
        so.FindProperty("painelVitoria").objectReferenceValue = painelVitoria;
        so.FindProperty("textoVitoria").objectReferenceValue  = textoVitoria;
        so.ApplyModifiedProperties();

        // ── 6. Cores do placar ────────────────────────────────────────────────
        textoJogador.color = Color.red;  // esquerda = vermelho
        textoIA.color      = Color.blue; // direita  = azul
        EditorUtility.SetDirty(textoJogador);
        EditorUtility.SetDirty(textoIA);

        // ── 7. Fundo da câmera = branco ─────────────────────────────────────────
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            Undo.RecordObject(cam, "Fundo branco");
            cam.backgroundColor = Color.white;
            EditorUtility.SetDirty(cam);
        }

        // ── 8. Cor da bola = branco (preserva as cores do sprite) ───────────
        GameObject bolaGO = GameObject.Find("Bola");
        if (bolaGO != null)
        {
            SpriteRenderer sr = bolaGO.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Cor da bola");
                sr.color = Color.white;
                EditorUtility.SetDirty(sr);
            }
        }
        else
        {
            Debug.LogWarning("[PlacarSetup] Objeto 'Bola' não encontrado. Renomeie o GameObject da bola para 'Bola'.");
        }

        // ── 9. Label do desenvolvedor (canto inferior direito) ───────────────
        CriarLabelDesenvolvedor(canvas.transform);

        // ── 10. Salvar e avisar ────────────────────────────────────────────────
        EditorUtility.SetDirty(placarCtrl);
        Debug.Log("[PlacarSetup] ✅ Placar configurado com sucesso!");
        Selection.activeGameObject = placarGO;
        EditorGUIUtility.PingObject(placarGO);
    }

    // ── Helper: cria a label do desenvolvedor no canto inferior direito ────────
    static void CriarLabelDesenvolvedor(Transform canvasTransform)
    {
        const string NOME_OBJ = "LabelDesenvolvedor";

        // Remove a existente para recriar (garante texto/posição sempre corretos)
        Transform existente = canvasTransform.Find(NOME_OBJ);
        if (existente != null)
            Undo.DestroyObjectImmediate(existente.gameObject);

        GameObject go = new GameObject(NOME_OBJ);
        go.transform.SetParent(canvasTransform, false);
        Undo.RegisterCreatedObjectUndo(go, "Criar LabelDesenvolvedor");

        // Ancora no canto inferior direito
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f); // canto inferior direito
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-10f, 8f); // margem de 10px da borda
        rt.sizeDelta        = new Vector2(280f, 36f);

        TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = "Frederico M.  |  fredelitosouza@gmail.com";
        txt.fontSize  = 11;
        txt.color     = new Color(0f, 0f, 0f, 0.45f); // preto semi-transparente (visível no fundo branco)
        txt.alignment = TextAlignmentOptions.BottomRight;
        txt.fontStyle = FontStyles.Italic;
    }

    // ── Helper: cria ou atualiza um TMP_Text como filho do parent ─────────────
    static TMP_Text CriarTexto(Transform parent, string nome, string conteudo,
                                Vector2 anchorMin, Vector2 anchorMax, Vector2 posicaoLocal)
    {
        // Reutiliza se já existir, mas atualiza tamanho e cor
        Transform existente = parent.Find(nome);
        if (existente != null)
        {
            TMP_Text tmp = existente.GetComponent<TMP_Text>();
            if (tmp != null) tmp.fontSize = 42;
            // cor será aplicada externamente após o retorno
            return tmp;
        }

        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Criar {nome}");

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicaoLocal;
        rt.sizeDelta        = new Vector2(150f, 70f);

        TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = conteudo;
        txt.fontSize  = 42;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;

        return txt;
    }
}
#endif
