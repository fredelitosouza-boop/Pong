using UnityEngine;

public class BolaController : MonoBehaviour
{
    public float velocidade = 7f;
    private Rigidbody2D meuRB;
    private bool pontoCont = false; // evita contar o ponto múltiplas vezes por gol
    private TrailRenderer trilha;  // rastro TrailRenderer (stripe)
    private GhostTrail ghostTrail; // rastro fantasma (cópias da sprite)

    [Header("Super Raquetada")]
    [Tooltip("Multiplicador de velocidade durante a super raquetada")]
    public float multiplicadorSuper = 2f;
    [Tooltip("Duração do boost em segundos")]
    public float duracaoSuper = 3f;
    [Tooltip("Porcentagem da extremidade da raquete que ativa o boost (0.0 a 1.0)")]
    public float limiteExtremo = 0.65f;

    [Header("Início de Jogo")]
    [Tooltip("Se marcado, o jogo inicia parado aguardando o toque na tela ou tecla ENTER")]
    public bool aguardarInicio = true;

    [Tooltip("Se marcado, aguarda toque/ENTER também após cada ponto marcado")]
    public bool aguardarAposPonto = false;

    [Tooltip("Aviso ou texto na tela com instrução para iniciar (opcional)")]
    public GameObject avisoIniciar;

    // Estado global indicando se a bola/jogo está em andamento
    public static bool JogoIniciado { get; private set; } = false;

    private float velocidadeBase; // guarda a velocidade original

    void Awake()
    {
        ConfigurarOrientacaoHorizontal();
    }

    /// <summary>
    /// Força e trava a orientação do jogo na horizontal (Landscape), permitindo virar o celular para ambos os lados horizontais.
    /// </summary>
    private void ConfigurarOrientacaoHorizontal()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    void Start()
    {
        meuRB      = GetComponent<Rigidbody2D>();
        trilha     = GetComponent<TrailRenderer>();
        ghostTrail = GetComponent<GhostTrail>();
        velocidadeBase = velocidade; // salva a velocidade original

        if (aguardarInicio)
        {
            PrepararParaIniciar();
        }
        else
        {
            IniciarJogo();
        }
    }

    void Update()
    {
        // Se o jogo ainda não iniciou, aguarda toque na tela ou ENTER
        if (!JogoIniciado)
        {
            if (VerificarInputInicio())
            {
                IniciarJogo();
            }
            return;
        }

        // Bola saiu pelo lado DIREITO → ponto para o Jogador (esquerda)
        if (!pontoCont && transform.position.x > 10f)
        {
            pontoCont = true;
            if (PlacarController.Instancia != null)
                PlacarController.Instancia.MarcarPontoJogador();
            else
                ReiniciarBola();
        }
        // Bola saiu pelo lado ESQUERDO → ponto para a IA (direita)
        else if (!pontoCont && transform.position.x < -10f)
        {
            pontoCont = true;
            if (PlacarController.Instancia != null)
                PlacarController.Instancia.MarcarPontoIA();
            else
                ReiniciarBola();
        }
    }

    void FixedUpdate()
    {
        if (!JogoIniciado) return;

        // Mantém a bola com a velocidade constante (sem desacelerar)
        if (meuRB.linearVelocity != Vector2.zero)
        {
            meuRB.linearVelocity = meuRB.linearVelocity.normalized * velocidade;
        }
    }

    /// <summary>
    /// Verifica toque na tela (touch / clique do mouse) ou tecla ENTER (Return / KeypadEnter).
    /// </summary>
    private bool VerificarInputInicio()
    {
        // Tecla ENTER do teclado convencional ou numérico
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;

        // Toque na tela (dispositivos móveis / touchscreen)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;

        // Clique do mouse (na Unity também captura toques na tela em modo de simulação/plataforma)
        if (Input.GetMouseButtonDown(0))
            return true;

#if ENABLE_INPUT_SYSTEM
        // Suporte adicional caso o projeto esteja utilizando o New Input System
        var teclado = UnityEngine.InputSystem.Keyboard.current;
        if (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame))
            return true;

        var telaToque = UnityEngine.InputSystem.Touchscreen.current;
        if (telaToque != null && telaToque.primaryTouch.press.wasPressedThisFrame)
            return true;

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;
#endif

        return false;
    }

    /// <summary>
    /// Inicia a movimentação da bola e desativa o aviso de início se houver.
    /// </summary>
    public void IniciarJogo()
    {
        JogoIniciado = true;

        if (avisoIniciar != null)
            avisoIniciar.SetActive(false);

        LancarBola();
    }

    /// <summary>
    /// Posiciona a bola parada no centro e exibe o aviso para aguardar toque ou ENTER.
    /// </summary>
    public void PrepararParaIniciar()
    {
        JogoIniciado = false;
        pontoCont = false;
        StopAllCoroutines();
        CancelInvoke();
        velocidade = velocidadeBase;

        transform.position = Vector3.zero;
        if (meuRB != null)
        {
            meuRB.linearVelocity = Vector2.zero;
            meuRB.angularVelocity = 0f;
        }

        // Limpa trilhas para não deixar rastro fantasma ao reiniciar
        if (trilha != null) { trilha.Clear(); trilha.emitting = false; }
        if (ghostTrail != null) ghostTrail.LimparRastro();

        if (avisoIniciar != null)
            avisoIniciar.SetActive(true);
    }

    public void ReiniciarBola()
    {
        pontoCont = false; // libera para contar o próximo ponto

        // Cancela qualquer boost ativo e restaura a velocidade base
        StopAllCoroutines();
        CancelInvoke();
        velocidade = velocidadeBase;

        // Reposiciona no centro e zera a velocidade
        transform.position = Vector3.zero;
        meuRB.linearVelocity = Vector2.zero;
        meuRB.angularVelocity = 0f;

        // Limpa o rastro da trilha para não cruzar a tela ao reiniciar
        if (trilha != null) { trilha.Clear(); trilha.emitting = false; }
        if (ghostTrail != null) ghostTrail.LimparRastro();

        if (aguardarAposPonto)
        {
            PrepararParaIniciar();
        }
        else
        {
            // Aguarda 0.5 segundo antes de lançar novamente
            Invoke(nameof(LancarBola), 0.5f);
        }
    }

    void LancarBola()
    {
        // Sorteia a direção horizontal: -1 (esquerda) ou 1 (direita)
        float direcaoX = Random.Range(0, 2) == 0 ? -1f : 1f;

        // Sorteia um ângulo vertical aleatório
        float direcaoY = Random.Range(-0.5f, 0.5f);

        Vector2 direcao = new Vector2(direcaoX, direcaoY).normalized;
        meuRB.linearVelocity = direcao * velocidade;

        // Reativa trilhas ao lançar
        if (trilha != null) trilha.emitting = true;
        if (ghostTrail != null) ghostTrail.AtivarRastro();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ao bater em uma raquete, ajusta o ângulo do rebote com base em onde acertou
        if (collision.gameObject.name.Contains("Raquete"))
        {
            // Diferença de altura entre a bola e o centro da raquete
            float diferencaY = transform.position.y - collision.transform.position.y;

            // Metade da altura da raquete (usa o collider para ser preciso)
            float meiaAltura = collision.collider.bounds.size.y / 2f;

            // Se bateu na raquete da esquerda manda pra direita (1), se na da direita manda pra esquerda (-1)
            float direcaoX = transform.position.x > collision.transform.position.x ? 1f : -1f;

            Vector2 novaDirecao = new Vector2(direcaoX, diferencaY).normalized;

            // ── Super Raquetada ──────────────────────────────────────────────
            // Verifica se bateu na extremidade (>= limiteExtremo % da meia altura)
            bool ehExtremo = meiaAltura > 0f && Mathf.Abs(diferencaY) >= meiaAltura * limiteExtremo;

            if (ehExtremo)
            {
                StopAllCoroutines(); // cancela boost anterior se houver
                StartCoroutine(BoostTemporario());
                meuRB.linearVelocity = novaDirecao * (velocidadeBase * multiplicadorSuper);
                Debug.Log("[SuperRaquetada] 💥 Boost ativado!");
            }
            else
            {
                meuRB.linearVelocity = novaDirecao * velocidade;
            }
        }
    }

    // Aplica o boost e restaura a velocidade após 'duracaoSuper' segundos
    private System.Collections.IEnumerator BoostTemporario()
    {
        velocidade = velocidadeBase * multiplicadorSuper;
        yield return new WaitForSeconds(duracaoSuper);
        velocidade = velocidadeBase;
        Debug.Log("[SuperRaquetada] Velocidade restaurada.");
    }
}
