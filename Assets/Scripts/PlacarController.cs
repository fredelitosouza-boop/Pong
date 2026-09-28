using UnityEngine;
using TMPro;

public class PlacarController : MonoBehaviour
{
    // ── Singleton para acesso global ─────────────────────────────────────────
    public static PlacarController Instancia { get; private set; }

    // ── Configurações expostas no Inspector ──────────────────────────────────
    [Header("Pontuação")]
    [Tooltip("Pontuação necessária para reiniciar o jogo")]
    public int pontosParaReiniciar = 5;

    [Header("Referências de UI")]
    [Tooltip("TextMeshPro do placar do jogador esquerdo (Jogador)")]
    public TMP_Text textoJogador;

    [Tooltip("TextMeshPro do placar do jogador direito (IA / 2P)")]
    public TMP_Text textoIA;

    [Tooltip("Painel de mensagem de vitória (opcional)")]
    public GameObject painelVitoria;

    [Tooltip("Texto da mensagem de vitória (opcional)")]
    public TMP_Text textoVitoria;

    [Header("Música de Fundo")]
    [Tooltip("Música de fundo a ser tocada em loop")]
    public AudioClip musicaDeFundo;

    [Range(0f, 1f)]
    [Tooltip("Volume da música de fundo")]
    public float volumeMusica = 0.5f;

    [Tooltip("Tocar a música continuamente em loop")]
    public bool loopMusica = true;

    private AudioSource audioSourceMusica;

    // ── Estado interno ───────────────────────────────────────────────────────
    private int pontosJogador = 0;
    private int pontosIA      = 0;

    // ── Unity Callbacks ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

#if UNITY_EDITOR
        if (musicaDeFundo == null)
        {
            CarregarMusicaEditor();
        }
#endif
    }

    void Start()
    {
        AtualizarTextos();

        if (painelVitoria != null)
            painelVitoria.SetActive(false);

        IniciarMusicaDeFundo();
    }

    // ── Métodos Públicos ─────────────────────────────────────────────────────

    /// <summary>
    /// Chame quando a bola sair pelo lado DIREITO (ponto para o Jogador da esquerda).
    /// </summary>
    public void MarcarPontoJogador()
    {
        pontosJogador++;
        AtualizarTextos();
        if (!VerificarVitoria())
            ReiniciarBolaNormal(); // jogo continua: relança a bola
    }

    /// <summary>
    /// Chame quando a bola sair pelo lado ESQUERDO (ponto para a IA / 2P).
    /// </summary>
    public void MarcarPontoIA()
    {
        pontosIA++;
        AtualizarTextos();
        if (!VerificarVitoria())
            ReiniciarBolaNormal(); // jogo continua: relança a bola
    }

    // ── Métodos Privados ─────────────────────────────────────────────────────

    void AtualizarTextos()
    {
        if (textoJogador != null)
            textoJogador.text = pontosJogador.ToString();

        if (textoIA != null)
            textoIA.text = pontosIA.ToString();
    }

    // Retorna true se alguém venceu, false se o jogo continua
    bool VerificarVitoria()
    {
        if (pontosJogador >= pontosParaReiniciar)
        {
            MostrarVitoria("Jogador Venceu!");
            return true;
        }
        else if (pontosIA >= pontosParaReiniciar)
        {
            MostrarVitoria("IA Venceu!");
            return true;
        }
        return false;
    }

    // Relança a bola normalmente (ponto simples, jogo continua)
    void ReiniciarBolaNormal()
    {
        BolaController bola = FindFirstObjectByType<BolaController>();
        if (bola != null)
            bola.ReiniciarBola();
    }

    void MostrarVitoria(string mensagem)
    {
        if (painelVitoria != null)
        {
            painelVitoria.SetActive(true);

            if (textoVitoria != null)
                textoVitoria.text = mensagem;
        }
        else
        {
            Debug.Log($"[Placar] {mensagem} — Reiniciando em 2 segundos...");
        }

        BolaController bola = FindFirstObjectByType<BolaController>();
        if (bola != null)
            bola.enabled = false;

        Invoke(nameof(ReiniciarPartida), 2f);
    }

    void ReiniciarPartida()
    {
        pontosJogador = 0;
        pontosIA      = 0;
        AtualizarTextos();

        if (painelVitoria != null)
            painelVitoria.SetActive(false);

        BolaController bola = FindFirstObjectByType<BolaController>();
        if (bola != null)
        {
            bola.enabled = true;

            // Respeita a flag aguardarInicio da bola:
            // Se aguardarInicio=true → PrepararParaIniciar() (aguarda toque/ENTER)
            // Se aguardarInicio=false → ReiniciarBola() (relança automaticamente)
            if (bola.aguardarInicio)
                bola.PrepararParaIniciar();
            else
                bola.ReiniciarBola();
        }
    }

    /// <summary>
    /// Configura e inicia a reprodução da música de fundo em loop contínuo (som 2D).
    /// </summary>
    void IniciarMusicaDeFundo()
    {
#if UNITY_EDITOR
        if (musicaDeFundo == null)
        {
            CarregarMusicaEditor();
        }
#endif

        if (musicaDeFundo == null)
        {
            Debug.LogWarning("[PlacarController] ⚠️ Nenhuma música de fundo encontrada ou atribuída.");
            return;
        }

        audioSourceMusica = gameObject.GetComponent<AudioSource>();
        if (audioSourceMusica == null)
        {
            audioSourceMusica = gameObject.AddComponent<AudioSource>();
        }

        audioSourceMusica.clip = musicaDeFundo;
        audioSourceMusica.volume = volumeMusica;
        audioSourceMusica.loop = loopMusica;
        audioSourceMusica.playOnAwake = true;
        audioSourceMusica.spatialBlend = 0f; // Som 2D puro (sem atenuação por distância)

        if (!audioSourceMusica.isPlaying)
        {
            audioSourceMusica.Play();
            Debug.Log($"[PlacarController] 🎵 Música '{musicaDeFundo.name}' iniciada em loop!");
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (musicaDeFundo == null)
        {
            CarregarMusicaEditor();
        }

        if (audioSourceMusica != null)
        {
            audioSourceMusica.volume = volumeMusica;
            audioSourceMusica.loop = loopMusica;
        }
    }

    private void CarregarMusicaEditor()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip Heroic Polonaise");
        if (guids.Length == 0)
            guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip");

        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            musicaDeFundo = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
