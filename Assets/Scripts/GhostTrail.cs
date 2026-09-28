using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efeito de rastro fantasma para a Bola do Pong:
/// a cada intervalo, cria uma cópia da sprite da bola com cor amarela
/// que vai desbotando até desaparecer — como um "ghost trail" arcade.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GhostTrail : MonoBehaviour
{
    [Header("Rastro Fantasma")]
    [Tooltip("Quantidade de fantasmas simultâneos visíveis")]
    [Range(2, 12)]
    public int quantidadeFantasmas = 6;

    [Tooltip("Intervalo em segundos entre cada fantasma gerado")]
    [Range(0.01f, 0.15f)]
    public float intervalo = 0.03f;

    [Tooltip("Tempo em segundos que cada fantasma dura antes de sumir")]
    [Range(0.05f, 0.5f)]
    public float duracaoFantasma = 0.18f;

    [Tooltip("Cor inicial do fantasma (com transparência)")]
    public Color corInicio = new Color(1f, 0.92f, 0f, 0.75f);   // amarelo ouro semi-transparente

    [Tooltip("Cor final do fantasma (totalmente transparente)")]
    public Color corFim = new Color(1f, 0.6f, 0f, 0f);          // laranja totalmente invisível

    [Tooltip("Tamanho do fantasma em relação à bola original (ex: 1.0 = igual, 0.8 = menor)")]
    [Range(0.4f, 1.2f)]
    public float escalaFantasma = 0.85f;

    [Tooltip("Ordem de renderização do rastro (abaixo da bola)")]
    public int sortingOrder = 0;

    // ── Controle interno ──────────────────────────────────────────────────────
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private float timer;
    private bool emitindo = false;

    // Pool de fantasmas (GameObjects reutilizáveis)
    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private readonly List<FantasmaAtivo> ativos = new List<FantasmaAtivo>();
    private Transform poolParent; // objeto vazio para organizar hierarquia

    private struct FantasmaAtivo
    {
        public SpriteRenderer sr;
        public float tempoNascimento;
    }

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Cria um parent vazio para manter a hierarquia limpa
        poolParent = new GameObject("[GhostPool]").transform;
        poolParent.SetParent(null); // fora da bola — fantasmas ficam no mundo

        // Pre-aloca o pool
        for (int i = 0; i < quantidadeFantasmas + 2; i++)
        {
            pool.Enqueue(CriarFantasmaNaPool());
        }
    }

    private GameObject CriarFantasmaNaPool()
    {
        GameObject go = new GameObject("Ghost");
        go.transform.SetParent(poolParent);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder   = sortingOrder;
        go.SetActive(false);
        return go;
    }

    // ── Ativar / desativar emissão ───────────────────────────────────────────

    /// <summary>Chame ao lançar a bola.</summary>
    public void AtivarRastro()
    {
        emitindo = true;
        timer = 0f;
    }

    /// <summary>Chame ao reiniciar / pausar a bola.</summary>
    public void LimparRastro()
    {
        emitindo = false;
        timer = 0f;

        // Desativa todos os fantasmas ativos imediatamente
        foreach (var f in ativos)
        {
            f.sr.gameObject.SetActive(false);
            pool.Enqueue(f.sr.gameObject);
        }
        ativos.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────

    void Update()
    {
        float agora = Time.time;

        // ── Atualiza fantasmas existentes ────────────────────────────────────
        for (int i = ativos.Count - 1; i >= 0; i--)
        {
            FantasmaAtivo f = ativos[i];
            float t = (agora - f.tempoNascimento) / duracaoFantasma;

            if (t >= 1f)
            {
                // Expirou — devolve ao pool
                f.sr.gameObject.SetActive(false);
                pool.Enqueue(f.sr.gameObject);
                ativos.RemoveAt(i);
                continue;
            }

            // Interpola cor de amarelo opaco → laranja transparente
            f.sr.color = Color.Lerp(corInicio, corFim, t);
        }

        // ── Emite um novo fantasma no intervalo ──────────────────────────────
        if (!emitindo || spriteRenderer.sprite == null) return;
        if (rb != null && rb.linearVelocity.sqrMagnitude < 0.01f) return;

        timer += Time.deltaTime;
        if (timer < intervalo) return;
        timer = 0f;

        // Reusa do pool ou cria novo se necessário
        GameObject go;
        if (pool.Count > 0)
        {
            go = pool.Dequeue();
        }
        else
        {
            go = CriarFantasmaNaPool();
        }

        // Configura o fantasma
        go.transform.position   = transform.position;
        go.transform.rotation   = transform.rotation;
        go.transform.localScale = transform.lossyScale * escalaFantasma;

        SpriteRenderer ghostSR = go.GetComponent<SpriteRenderer>();
        ghostSR.sprite       = spriteRenderer.sprite;
        ghostSR.color        = corInicio;
        ghostSR.sortingOrder = sortingOrder;

        go.SetActive(true);

        ativos.Add(new FantasmaAtivo { sr = ghostSR, tempoNascimento = agora });
    }

    void OnDestroy()
    {
        // Limpa o pool ao destruir a bola (ex: troca de cena)
        if (poolParent != null)
            Destroy(poolParent.gameObject);
    }
}
