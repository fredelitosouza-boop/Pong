using UnityEngine;

/// <summary>
/// Ajusta o Background automaticamente para qualquer tela e resolução Android
/// (16:9, 18:9, 19.5:9, 20:9, 21:9, tablets 4:3, telas dobráveis e notch/cutouts),
/// garantindo modo Cover sem cortes feios, sem bordas pretas e sem distorção.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    public enum ModoAjuste
    {
        Cover,   // Preenche 100% da tela sem bordas pretas (recomendado para background)
        Fit,     // Mostra a imagem inteira sem cortar nada (pode deixar barras pretas)
        Stretch  // Estica para preencher exatamente largura e altura (pode distorcer)
    }

    [Header("Configuração de Enquadramento")]
    [Tooltip("Modo de preenchimento para telas mobile.")]
    public ModoAjuste modo = ModoAjuste.Cover;

    [Tooltip("Margem extra (ex: 1.02 = +2%) para evitar frestas de 1 pixel devido a arredondamento subpixel ou safe area no Android.")]
    [Range(1f, 1.15f)]
    public float margemSeguranca = 1.02f;

    [Tooltip("Acompanhar o centro da câmera se ela se movimentar.")]
    public bool seguirCamera = true;

    [Header("Referências")]
    [SerializeField] private Camera _cameraAlvo;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    // Cache para evitar recalcular todo frame sem necessidade
    private int _ultimoWidth;
    private int _ultimoHeight;
    private float _ultimoOrthoSize;
    private float _ultimoAspect;
    private ScreenOrientation _ultimaOrientacao;

    private void Awake()
    {
        ObterReferencias();
        AjustarAgora();
    }

    private void OnEnable()
    {
        ObterReferencias();
        AjustarAgora();
    }

    private void Start()
    {
        ObterReferencias();
        AjustarAgora();
    }

    private void LateUpdate()
    {
        // Detecta mudança de resolução, rotação do aparelho ou redimensionamento de janela
        if (_cameraAlvo == null) _cameraAlvo = ObterCamera();

        if (_cameraAlvo == null) return;

        bool mudou = Screen.width != _ultimoWidth ||
                     Screen.height != _ultimoHeight ||
                     Screen.orientation != _ultimaOrientacao ||
                     !Mathf.Approximately(_cameraAlvo.orthographicSize, _ultimoOrthoSize) ||
                     !Mathf.Approximately(_cameraAlvo.aspect, _ultimoAspect);

        if (mudou)
        {
            AjustarAgora();
        }
        else if (seguirCamera && _cameraAlvo != null)
        {
            Vector3 camPos = _cameraAlvo.transform.position;
            if (transform.position.x != camPos.x || transform.position.y != camPos.y)
            {
                transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
            }
        }
    }

    public void AjustarAgora()
    {
        ObterReferencias();

        if (_cameraAlvo == null || _spriteRenderer == null || _spriteRenderer.sprite == null)
            return;

        // Dimensões do viewport da câmera ortográfica em unidades Unity
        float camHeight = _cameraAlvo.orthographicSize * 2f;
        float camWidth = camHeight * _cameraAlvo.aspect;

        // Dimensões originais do sprite (em unidades Unity, levando em conta o PPU)
        Vector2 spriteSize = _spriteRenderer.sprite.rect.size / _spriteRenderer.sprite.pixelsPerUnit;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        float escalaX = camWidth / spriteSize.x;
        float escalaY = camHeight / spriteSize.y;

        Vector3 novaEscala = Vector3.one;

        switch (modo)
        {
            case ModoAjuste.Cover:
                // Pega a maior escala para garantir cobertura total (sem barras pretas)
                float escalaCover = Mathf.Max(escalaX, escalaY) * margemSeguranca;
                novaEscala = new Vector3(escalaCover, escalaCover, 1f);
                break;

            case ModoAjuste.Fit:
                // Pega a menor escala para que o sprite caiba 100% visível
                float escalaFit = Mathf.Min(escalaX, escalaY);
                novaEscala = new Vector3(escalaFit, escalaFit, 1f);
                break;

            case ModoAjuste.Stretch:
                // Estica independente em X e Y
                novaEscala = new Vector3(escalaX * margemSeguranca, escalaY * margemSeguranca, 1f);
                break;
        }

        transform.localScale = novaEscala;

        if (seguirCamera)
        {
            Vector3 camPos = _cameraAlvo.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, 0f);
        }

        // Atualiza cache
        _ultimoWidth = Screen.width;
        _ultimoHeight = Screen.height;
        _ultimaOrientacao = Screen.orientation;
        _ultimoOrthoSize = _cameraAlvo.orthographicSize;
        _ultimoAspect = _cameraAlvo.aspect;
    }

    private void ObterReferencias()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_cameraAlvo == null)
            _cameraAlvo = ObterCamera();
    }

    private Camera ObterCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            cam = Object.FindFirstObjectByType<Camera>();
        return cam;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AjustarAgora();
    }
#endif
}
