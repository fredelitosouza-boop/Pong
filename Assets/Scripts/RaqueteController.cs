using UnityEngine;

public class RaqueteController : MonoBehaviour
{
    // Criando meu vector3
    private Vector3 minhaPosicao;
    public float meuY;
    public float velocidade = 5f;
    public float meuLimite = 3.5f;

    // Define se esta raquete é a da esquerda
    public bool raqueteDaEsquerda = true;

    // Para a raquete da direita: define se é controlada por IA
    public bool ehIA = true;

    // Referência do Transform da bola para a IA seguir
    public Transform transformBola;
    
    [Header("Controle Touch / Mobile")]
    [Tooltip("Ativa o controle da raquete via toque na tela ou clique do mouse")]
    public bool controlePorTouch = true;

    [Tooltip("Se verdadeiro, a raquete acompanha a altura do dedo diretamente. Se falso, move suavemente")]
    public bool seguirDedoInstantaneamente = true;

    [Tooltip("Velocidade de movimento pelo touch quando não for instantâneo")]
    public float velocidadeTouch = 30f;

    [Tooltip("No modo contra a IA (1P), permite tocar em qualquer lugar da tela para mover a raquete")]
    public bool toqueTelaInteiraNoModoIA = true;

    private Camera camPrincipal;
    private bool ehModoContraIA;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Pega a posição inicial da raquete (garante que a raquete esquerda fique na esquerda e a direita na direita)
        minhaPosicao = transform.position;
        meuY = transform.position.y;
        camPrincipal = Camera.main;

        // Se for a raquete da direita (IA) e a bola não foi atribuída no Inspector, procura automaticamente na cena
        if (!raqueteDaEsquerda && transformBola == null)
        {
            GameObject bolaObj = GameObject.Find("Bola");
            if (bolaObj != null)
            {
                transformBola = bolaObj.transform;
            }
        }

        // Verifica se o jogo está no modo contra IA
        RaqueteController[] raquetes = FindObjectsByType<RaqueteController>(FindObjectsSortMode.None);
        foreach (var r in raquetes)
        {
            if (!r.raqueteDaEsquerda && r.ehIA)
            {
                ehModoContraIA = true;
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool moveuComTouch = false;

        // Processa o movimento por touch se estiver habilitado
        if (controlePorTouch)
        {
            moveuComTouch = ProcessarTouch();
        }

        // Se for a raquete da esquerda, ela será controlada pelas setinhas (caso não haja touch neste frame)
        if (raqueteDaEsquerda)
        {
            if (!moveuComTouch)
            {
                // Pegando a setinha pra cima
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    meuY += velocidade * Time.deltaTime;
                }

                // Pegando a setinha para baixo
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    meuY -= velocidade * Time.deltaTime;
                }
            }
        }
        else
        {
            // Raquete da direita
            if (ehIA)
            {
                // Inteligência Artificial: segue a bola apenas quando o jogo tiver iniciado
                if (BolaController.JogoIniciado && transformBola != null)
                {
                    meuY = Mathf.MoveTowards(meuY, transformBola.position.y, velocidade * Time.deltaTime);
                }
            }
            else
            {
                // Caso queira jogar de 2 jogadores (teclas W e S ou touch no lado direito)
                if (!moveuComTouch)
                {
                    if (Input.GetKey(KeyCode.W))
                    {
                        meuY += velocidade * Time.deltaTime;
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        meuY -= velocidade * Time.deltaTime;
                    }
                }
            }
        }

        // Limitando a raquete na tela para não sumir
        meuY = Mathf.Clamp(meuY, -meuLimite, meuLimite);

        // Modificar a posição da minha raquete  
        minhaPosicao.y = meuY;
        transform.position = minhaPosicao;
    }

    /// <summary>
    /// Processa toques na tela e cliques/arrastes de mouse.
    /// </summary>
    private bool ProcessarTouch()
    {
        if (camPrincipal == null)
            camPrincipal = Camera.main;
        if (camPrincipal == null) return false;

        // 1. Toques em dispositivos móveis (Touchscreen)
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    if (EstaNaAreaDeControle(touch.position))
                    {
                        MoverParaPosicao(touch.position);
                        return true;
                    }
                }
            }
        }

        // 2. Clique / arraste com mouse (útil para testes no Unity Editor)
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (EstaNaAreaDeControle(mousePos))
            {
                MoverParaPosicao(mousePos);
                return true;
            }
        }

#if ENABLE_INPUT_SYSTEM
        // 3. Suporte ao New Input System
        var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.isPressed)
                {
                    Vector2 pos = touch.position.ReadValue();
                    if (EstaNaAreaDeControle(pos))
                    {
                        MoverParaPosicao(pos);
                        return true;
                    }
                }
            }
        }
#endif

        return false;
    }

    /// <summary>
    /// Define se a posição na tela pertence à área de controle desta raquete.
    /// </summary>
    private bool EstaNaAreaDeControle(Vector2 screenPos)
    {
        if (raqueteDaEsquerda)
        {
            // No modo contra IA (1 Jogador), permite tocar em qualquer lugar da tela
            if (ehModoContraIA && toqueTelaInteiraNoModoIA)
                return true;

            // No modo 2 jogadores, apenas a metade esquerda da tela controla a raquete 1
            return screenPos.x <= Screen.width * 0.5f;
        }
        else
        {
            // Raquete da direita: só aceita controle se for 2º jogador humano (não IA) na metade direita
            if (ehIA) return false;

            return screenPos.x > Screen.width * 0.5f;
        }
    }

    /// <summary>
    /// Converte a posição do toque da tela para coordenadas do mundo e atualiza meuY.
    /// </summary>
    private void MoverParaPosicao(Vector2 screenPos)
    {
        Vector3 worldPoint = camPrincipal.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -camPrincipal.transform.position.z));
        float targetY = worldPoint.y;

        if (seguirDedoInstantaneamente)
        {
            meuY = targetY;
        }
        else
        {
            meuY = Mathf.MoveTowards(meuY, targetY, velocidadeTouch * Time.deltaTime);
        }
    }
}
