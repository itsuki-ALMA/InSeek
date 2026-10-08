using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Protótipo autocontido de Snake. Arena, sprites e interface são criados em tempo de execução.
/// </summary>
public class SerpenteDaMasmorra : MonoBehaviour
{
    private enum Estado { Menu, Jogando, Pausado, Fim, Vitoria }

    private const int LarguraGrade = 24;
    private const int AlturaGrade = 16;
    private const int PixelsPorCelula = 32;
    private const float TamanhoCelula = 0.5f;
    private const float CentroArenaY = -0.25f;
    private const float VelocidadeInicial = 4f;
    private const float VelocidadeMaxima = 8f;
    private const int PontosPorMaca = 10;
    private const string ChaveRecorde = "SerpenteDaMasmorra.Recorde";

    private readonly List<Vector2Int> celulasSerpente = new List<Vector2Int>();
    private readonly List<Vector2Int> inicioInterpolacao = new List<Vector2Int>();
    private readonly List<SpriteRenderer> segmentos = new List<SpriteRenderer>();

    private Estado estado = Estado.Menu;
    private Vector2Int direcao = Vector2Int.right;
    private Vector2Int direcaoNaFila = Vector2Int.right;
    private Vector2Int celulaMaca;
    private bool temDirecaoNaFila;
    private int pontuacao;
    private int recorde;
    private int macasComidas;
    private float velocidade = VelocidadeInicial;
    private float acumulador;

    private Transform raizMundo;
    private SpriteRenderer renderMaca;
    private Sprite spriteSolido;
    private Sprite spritePiso;
    private Sprite spriteCorpo;
    private Sprite spriteCabeca;
    private Sprite spriteMaca;
    private Sprite spriteBrilho;

    private Texture2D texturaGuiBranca;
    private Texture2D texturaPainel;
    private Texture2D texturaBorda;
    private Texture2D texturaBotao;
    private Texture2D texturaBotaoHover;
    private Texture2D texturaSobreposicao;
    private GUIStyle estiloTitulo;
    private GUIStyle estiloTituloPainel;
    private GUIStyle estiloCorpo;
    private GUIStyle estiloPequeno;
    private GUIStyle estiloHud;
    private GUIStyle estiloBotao;

    private float IntervaloMovimento { get { return 1f / velocidade; } }
    private float LarguraTelaReferencia { get { return Screen.width / EscalaGui; } }
    private float AlturaTelaReferencia { get { return Screen.height / EscalaGui; } }
    private float EscalaGui { get { return Mathf.Clamp(Screen.height / 1080f, 0.55f, 1.6f); } }

    private void Awake()
    {
        recorde = PlayerPrefs.GetInt(ChaveRecorde, 0);
        PrepararCamera();
        CriarSprites();
        CriarArena();
        renderMaca = CriarRender("Maçã", spriteMaca, Color.white, 4);
        ReiniciarRodada(false);
        PosicionarMaca();
    }

    private void Update()
    {
        TratarComandosDeEstado();
        TratarComandoDeDirecao();

        if (estado != Estado.Jogando)
            return;

        acumulador += Time.deltaTime;
        int passosDeSeguranca = 0;
        while (acumulador >= IntervaloMovimento && estado == Estado.Jogando && passosDeSeguranca < 5)
        {
            acumulador -= IntervaloMovimento;
            if (!AvancarUmaCelula())
                break;
            passosDeSeguranca++;
        }

        AtualizarPosicoesVisuais(Mathf.Clamp01(acumulador / IntervaloMovimento));
    }

    private void PrepararCamera()
    {
        Camera cameraJogo = Camera.main;
        if (cameraJogo == null)
        {
            GameObject objetoCamera = new GameObject("Main Camera");
            objetoCamera.tag = "MainCamera";
            cameraJogo = objetoCamera.AddComponent<Camera>();
        }
        cameraJogo.orthographic = true;
        cameraJogo.orthographicSize = 5.2f;
        cameraJogo.transform.position = new Vector3(0f, 0f, -10f);
        cameraJogo.clearFlags = CameraClearFlags.SolidColor;
        cameraJogo.backgroundColor = new Color32(10, 15, 18, 255);
    }

    private void CriarSprites()
    {
        spriteSolido = CriarSprite(CriarTexturaSolida(new Color32(255, 255, 255, 255)), 1f);
        spritePiso = CriarSprite(CriarTexturaPiso(), PixelsPorCelula * 2f);
        spriteCorpo = CriarSprite(CriarTexturaCorpo(false), 64f);
        spriteCabeca = CriarSprite(CriarTexturaCorpo(true), 64f);
        spriteMaca = CriarSprite(CriarTexturaMaca(), 64f);
        spriteBrilho = CriarSprite(CriarTexturaBrilho(), 64f);
    }

    private void CriarArena()
    {
        GameObject raiz = new GameObject("Arena da Masmorra");
        raizMundo = raiz.transform;
        raizMundo.position = Vector3.zero;

        CriarRetangulo("Sombra da arena", 0f, CentroArenaY, 13.1f, 9.05f, new Color32(4, 7, 9, 255), -12);
        SpriteRenderer piso = CriarRender("Piso de pedra", spritePiso, Color.white, -10);
        piso.transform.position = new Vector3(0f, CentroArenaY, 0f);
        CriarParedes();
        CriarTochas();
    }

    private void CriarParedes()
    {
        Color32 pedraEscura = new Color32(53, 70, 78, 255);
        Color32 pedraClara = new Color32(72, 91, 97, 255);
        Color32 madeira = new Color32(94, 58, 35, 255);
        float meioLargura = LarguraGrade * TamanhoCelula * 0.5f;
        float meioAltura = AlturaGrade * TamanhoCelula * 0.5f;

        CriarRetangulo("Moldura externa de madeira", 0f, CentroArenaY, 12.85f, 8.65f, madeira, -8);
        CriarRetangulo("Faixa de pedra superior", 0f, CentroArenaY + meioAltura + 0.06f, 12.35f, 0.30f, pedraEscura, -6);
        CriarRetangulo("Faixa de pedra inferior", 0f, CentroArenaY - meioAltura - 0.06f, 12.35f, 0.30f, pedraEscura, -6);
        CriarRetangulo("Faixa de pedra esquerda", -meioLargura - 0.06f, CentroArenaY, 0.30f, 8.25f, pedraEscura, -6);
        CriarRetangulo("Faixa de pedra direita", meioLargura + 0.06f, CentroArenaY, 0.30f, 8.25f, pedraEscura, -6);

        float ySuperior = CentroArenaY + meioAltura + 0.06f;
        float yInferior = CentroArenaY - meioAltura - 0.06f;
        for (int x = 0; x < LarguraGrade; x++)
        {
            float wx = (x - (LarguraGrade - 1) * 0.5f) * TamanhoCelula;
            Color cor = (x % 2 == 0) ? pedraClara : pedraEscura;
            CriarRetangulo("Bloco superior", wx, ySuperior, 0.47f, 0.28f, cor, -5);
            CriarRetangulo("Bloco inferior", wx, yInferior, 0.47f, 0.28f, cor, -5);
        }
        for (int y = 0; y < AlturaGrade; y++)
        {
            float wy = CentroArenaY + (y - (AlturaGrade - 1) * 0.5f) * TamanhoCelula;
            Color cor = (y % 2 == 0) ? pedraClara : pedraEscura;
            CriarRetangulo("Bloco esquerdo", -meioLargura - 0.06f, wy, 0.28f, 0.47f, cor, -5);
            CriarRetangulo("Bloco direito", meioLargura + 0.06f, wy, 0.28f, 0.47f, cor, -5);
        }

        Color32 capitel = new Color32(126, 93, 51, 255);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                CriarRetangulo("Capitel de canto", sx * (meioLargura + 0.06f), CentroArenaY + sy * (meioAltura + 0.06f), 0.52f, 0.52f, capitel, -3);
            }
        }
    }

    private void CriarTochas()
    {
        for (int lado = -1; lado <= 1; lado += 2)
        {
            float y = CentroArenaY + lado * 4.35f;
            for (int x = -1; x <= 1; x += 2)
            {
                float wx = x * 4.35f;
                SpriteRenderer brilho = CriarRender("Luz quente da tocha", spriteBrilho, new Color32(245, 145, 54, 110), -4);
                brilho.transform.position = new Vector3(wx, y, 0f);
                brilho.transform.localScale = new Vector3(0.68f, 0.68f, 1f);
                CriarRetangulo("Chama da tocha", wx, y, 0.12f, 0.22f, new Color32(255, 192, 82, 255), -2);
            }
        }
    }

    private void CriarRetangulo(string nome, float x, float y, float largura, float altura, Color cor, int ordem)
    {
        SpriteRenderer sr = CriarRender(nome, spriteSolido, cor, ordem);
        sr.transform.position = new Vector3(x, y, 0f);
        sr.transform.localScale = new Vector3(largura, altura, 1f);
    }

    private SpriteRenderer CriarRender(string nome, Sprite sprite, Color cor, int ordem)
    {
        GameObject objeto = new GameObject(nome);
        if (raizMundo != null)
            objeto.transform.SetParent(raizMundo, false);
        SpriteRenderer sr = objeto.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = cor;
        sr.sortingOrder = ordem;
        return sr;
    }

    private void ReiniciarRodada(bool iniciar)
    {
        for (int i = 0; i < segmentos.Count; i++)
        {
            if (segmentos[i] != null)
                Destroy(segmentos[i].gameObject);
        }
        segmentos.Clear();
        celulasSerpente.Clear();
        inicioInterpolacao.Clear();
        direcao = Vector2Int.right;
        direcaoNaFila = Vector2Int.right;
        temDirecaoNaFila = false;
        pontuacao = 0;
        macasComidas = 0;
        velocidade = VelocidadeInicial;
        acumulador = 0f;

        Vector2Int centro = new Vector2Int(LarguraGrade / 2, AlturaGrade / 2);
        celulasSerpente.Add(centro);
        celulasSerpente.Add(centro + Vector2Int.left);
        celulasSerpente.Add(centro + Vector2Int.left * 2);
        for (int i = 0; i < celulasSerpente.Count; i++)
            segmentos.Add(CriarRender("Segmento da serpente", i == 0 ? spriteCabeca : spriteCorpo, Color.white, 5));

        inicioInterpolacao.AddRange(celulasSerpente);
        AtualizarPosicoesVisuais(1f);
        estado = iniciar ? Estado.Jogando : Estado.Menu;
    }

    private void ComecarPartida()
    {
        ReiniciarRodada(true);
        PosicionarMaca();
    }

    private void VoltarAoMenu()
    {
        ReiniciarRodada(false);
        PosicionarMaca();
    }

    private bool AvancarUmaCelula()
    {
        if (temDirecaoNaFila)
        {
            direcao = direcaoNaFila;
            temDirecaoNaFila = false;
        }

        Vector2Int proxima = celulasSerpente[0] + direcao;
        bool comeu = proxima == celulaMaca;
        if (proxima.x < 0 || proxima.x >= LarguraGrade || proxima.y < 0 || proxima.y >= AlturaGrade)
        {
            EncerrarPartida();
            return false;
        }

        int quantidadeVerificada = comeu ? celulasSerpente.Count : celulasSerpente.Count - 1;
        for (int i = 0; i < quantidadeVerificada; i++)
        {
            if (celulasSerpente[i] == proxima)
            {
                EncerrarPartida();
                return false;
            }
        }

        inicioInterpolacao.Clear();
        inicioInterpolacao.AddRange(celulasSerpente);
        celulasSerpente.Insert(0, proxima);
        if (comeu)
        {
            pontuacao += PontosPorMaca;
            macasComidas++;
            velocidade = Mathf.Min(VelocidadeMaxima, VelocidadeInicial + (macasComidas / 5) * 0.5f);
            if (pontuacao > recorde)
            {
                recorde = pontuacao;
                PlayerPrefs.SetInt(ChaveRecorde, recorde);
                PlayerPrefs.Save();
            }
            segmentos.Add(CriarRender("Segmento da serpente", spriteCorpo, Color.white, 5));
        }
        else
        {
            celulasSerpente.RemoveAt(celulasSerpente.Count - 1);
        }

        while (inicioInterpolacao.Count < celulasSerpente.Count)
            inicioInterpolacao.Add(inicioInterpolacao[inicioInterpolacao.Count - 1]);

        if (comeu)
            PosicionarMaca();

        AtualizarPosicoesVisuais(0f);
        return estado == Estado.Jogando;
    }

    private void EncerrarPartida()
    {
        estado = Estado.Fim;
        if (pontuacao > recorde)
        {
            recorde = pontuacao;
            PlayerPrefs.SetInt(ChaveRecorde, recorde);
            PlayerPrefs.Save();
        }
    }

    private void PosicionarMaca()
    {
        List<Vector2Int> livres = new List<Vector2Int>();
        for (int y = 0; y < AlturaGrade; y++)
        {
            for (int x = 0; x < LarguraGrade; x++)
            {
                Vector2Int candidata = new Vector2Int(x, y);
                if (!celulasSerpente.Contains(candidata))
                    livres.Add(candidata);
            }
        }

        if (livres.Count == 0)
        {
            estado = Estado.Vitoria;
            return;
        }

        celulaMaca = livres[Random.Range(0, livres.Count)];
        if (renderMaca != null)
        {
            renderMaca.transform.position = PontoDaCelula(celulaMaca);
            renderMaca.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
        }
    }

    private void AtualizarPosicoesVisuais(float interpolacao)
    {
        for (int i = 0; i < segmentos.Count && i < celulasSerpente.Count; i++)
        {
            if (segmentos[i] == null)
                continue;
            Vector2Int origem = i < inicioInterpolacao.Count ? inicioInterpolacao[i] : celulasSerpente[i];
            Vector3 de = PontoDaCelula(origem);
            Vector3 para = PontoDaCelula(celulasSerpente[i]);
            segmentos[i].transform.position = Vector3.Lerp(de, para, interpolacao);
            segmentos[i].transform.localScale = new Vector3(i == 0 ? 0.45f : 0.42f, i == 0 ? 0.45f : 0.42f, 1f);
            if (i == 0)
            {
                float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
                segmentos[i].transform.rotation = Quaternion.Euler(0f, 0f, angulo);
                segmentos[i].sprite = spriteCabeca;
            }
            else
            {
                segmentos[i].sprite = spriteCorpo;
            }
        }
    }

    private Vector3 PontoDaCelula(Vector2Int celula)
    {
        float x = (celula.x - (LarguraGrade - 1) * 0.5f) * TamanhoCelula;
        float y = CentroArenaY + (celula.y - (AlturaGrade - 1) * 0.5f) * TamanhoCelula;
        return new Vector3(x, y, 0f);
    }

    private void TratarComandoDeDirecao()
    {
        if (estado != Estado.Jogando || temDirecaoNaFila)
            return;
        if (CimaPressionado()) EnfileirarDirecao(Vector2Int.up);
        else if (BaixoPressionado()) EnfileirarDirecao(Vector2Int.down);
        else if (EsquerdaPressionada()) EnfileirarDirecao(Vector2Int.left);
        else if (DireitaPressionada()) EnfileirarDirecao(Vector2Int.right);
    }

    private void EnfileirarDirecao(Vector2Int candidata)
    {
        if (candidata.x * direcao.x + candidata.y * direcao.y == -1)
            return;
        direcaoNaFila = candidata;
        temDirecaoNaFila = true;
    }

    private void TratarComandosDeEstado()
    {
        if (estado == Estado.Menu && ConfirmarPressionado())
        {
            ComecarPartida();
            return;
        }
        if ((estado == Estado.Fim || estado == Estado.Vitoria) && (ConfirmarPressionado() || ReiniciarPressionado()))
        {
            ComecarPartida();
            return;
        }
        if ((estado == Estado.Fim || estado == Estado.Vitoria || estado == Estado.Pausado) && MenuPressionado())
        {
            VoltarAoMenu();
            return;
        }
        if (PausarPressionado())
        {
            if (estado == Estado.Jogando) estado = Estado.Pausado;
            else if (estado == Estado.Pausado) estado = Estado.Jogando;
        }
    }

    private bool CimaPressionado()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
        return pressionado;
    }
    private bool BaixoPressionado()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
        return pressionado;
    }
    private bool EsquerdaPressionada()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#endif
        return pressionado;
    }
    private bool DireitaPressionada()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
        return pressionado;
    }
    private bool ConfirmarPressionado()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        return pressionado;
    }
    private bool PausarPressionado()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space);
#endif
        return pressionado;
    }
    private bool ReiniciarPressionado()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.rKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.R);
#endif
        return pressionado;
    }
    private bool MenuPressionado()
    {
        bool pressionado = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) pressionado |= Keyboard.current.mKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressionado |= Input.GetKeyDown(KeyCode.M);
#endif
        return pressionado;
    }

    private void OnGUI()
    {
        PrepararEstilosGui();
        Matrix4x4 matrizAnterior = GUI.matrix;
        float escala = EscalaGui;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
        GUI.color = Color.white;
        float largura = LarguraTelaReferencia;
        float altura = AlturaTelaReferencia;

        if (estado == Estado.Jogando || estado == Estado.Pausado)
            DesenharHud(largura);

        if (estado == Estado.Menu)
            DesenharMenu(largura, altura);
        else if (estado == Estado.Pausado)
            DesenharPausa(largura, altura);
        else if (estado == Estado.Fim)
            DesenharFim(largura, altura);
        else if (estado == Estado.Vitoria)
            DesenharVitoria(largura, altura);

        GUI.color = Color.white;
        GUI.matrix = matrizAnterior;
    }

    private void PrepararEstilosGui()
    {
        if (estiloTitulo != null)
            return;

        texturaGuiBranca = CriarTexturaSolida(new Color32(255, 255, 255, 255));
        texturaPainel = CriarTexturaSolida(new Color32(22, 30, 32, 246));
        texturaBorda = CriarTexturaSolida(new Color32(183, 130, 59, 255));
        texturaBotao = CriarTexturaSolida(new Color32(68, 91, 69, 255));
        texturaBotaoHover = CriarTexturaSolida(new Color32(92, 122, 83, 255));
        texturaSobreposicao = CriarTexturaSolida(new Color32(4, 8, 10, 180));

        estiloTitulo = NovoEstilo(54, new Color32(245, 210, 135, 255), FontStyle.Bold, TextAnchor.MiddleCenter);
        estiloTituloPainel = NovoEstilo(36, new Color32(245, 210, 135, 255), FontStyle.Bold, TextAnchor.MiddleCenter);
        estiloCorpo = NovoEstilo(22, new Color32(224, 228, 214, 255), FontStyle.Normal, TextAnchor.MiddleCenter);
        estiloPequeno = NovoEstilo(17, new Color32(193, 201, 190, 255), FontStyle.Normal, TextAnchor.MiddleCenter);
        estiloHud = NovoEstilo(22, new Color32(244, 222, 171, 255), FontStyle.Bold, TextAnchor.MiddleLeft);

        estiloBotao = new GUIStyle(GUI.skin.button);
        estiloBotao.font = GUI.skin.font;
        estiloBotao.fontSize = 22;
        estiloBotao.fontStyle = FontStyle.Bold;
        estiloBotao.alignment = TextAnchor.MiddleCenter;
        estiloBotao.normal.background = texturaBotao;
        estiloBotao.hover.background = texturaBotaoHover;
        estiloBotao.active.background = texturaBorda;
        estiloBotao.normal.textColor = new Color32(250, 246, 229, 255);
        estiloBotao.hover.textColor = Color.white;
        estiloBotao.active.textColor = Color.white;
        estiloBotao.border = new RectOffset(10, 10, 10, 10);
        estiloBotao.padding = new RectOffset(12, 12, 8, 8);
    }

    private GUIStyle NovoEstilo(int tamanho, Color cor, FontStyle fonte, TextAnchor alinhamento)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.font = GUI.skin.font;
        estilo.fontSize = tamanho;
        estilo.fontStyle = fonte;
        estilo.alignment = alinhamento;
        estilo.wordWrap = true;
        estilo.normal.textColor = cor;
        return estilo;
    }

    private void DesenharHud(float largura)
    {
        GUI.color = new Color32(15, 22, 23, 245);
        GUI.DrawTexture(new Rect(0f, 0f, largura, 92f), texturaGuiBranca);
        GUI.color = new Color32(184, 132, 61, 255);
        GUI.DrawTexture(new Rect(0f, 88f, largura, 3f), texturaGuiBranca);
        GUI.color = Color.white;
        GUI.Label(new Rect(48f, 12f, 240f, 28f), "PONTOS", estiloPequeno);
        GUI.Label(new Rect(48f, 39f, 240f, 43f), pontuacao.ToString("D4"), estiloHud);
        GUI.Label(new Rect(largura - 290f, 12f, 240f, 28f), "RECORDE", estiloPequeno);
        GUI.Label(new Rect(largura - 290f, 39f, 240f, 43f), recorde.ToString("D4"), estiloHud);
        GUI.Label(new Rect(largura * 0.5f - 250f, 28f, 500f, 36f), "SERPENTE DA MASMORRA", estiloPequeno);
    }

    private void DesenharMenu(float largura, float altura)
    {
        DesenharSobreposicao(largura, altura);
        Rect painel = RetanguloCentral(largura, altura, 720f, 620f);
        DesenharPainel(painel);
        GUI.Label(new Rect(painel.x + 40f, painel.y + 54f, painel.width - 80f, 130f), "SERPENTE DA\nMASMORRA", estiloTitulo);
        GUI.Label(new Rect(painel.x + 65f, painel.y + 206f, painel.width - 130f, 62f), "Colete maçãs encantadas e sobreviva à masmorra.", estiloCorpo);
        if (GUI.Button(new Rect(painel.x + 145f, painel.y + 300f, painel.width - 290f, 68f), "JOGAR", estiloBotao))
            ComecarPartida();
        GUI.Label(new Rect(painel.x + 50f, painel.y + 394f, painel.width - 100f, 36f), "RECORDE  " + recorde.ToString("D4"), estiloCorpo);
        GUI.Label(new Rect(painel.x + 45f, painel.y + 456f, painel.width - 90f, 90f), "Mover: setas ou W A S D\nPausar: Esc ou Espaço", estiloPequeno);
    }

    private void DesenharPausa(float largura, float altura)
    {
        DesenharSobreposicao(largura, altura);
        Rect painel = RetanguloCentral(largura, altura, 620f, 440f);
        DesenharPainel(painel);
        GUI.Label(new Rect(painel.x + 35f, painel.y + 36f, painel.width - 70f, 62f), "JOGO PAUSADO", estiloTituloPainel);
        if (GUI.Button(new Rect(painel.x + 110f, painel.y + 132f, painel.width - 220f, 58f), "CONTINUAR", estiloBotao)) estado = Estado.Jogando;
        if (GUI.Button(new Rect(painel.x + 110f, painel.y + 212f, painel.width - 220f, 58f), "REINICIAR", estiloBotao)) ComecarPartida();
        if (GUI.Button(new Rect(painel.x + 110f, painel.y + 292f, painel.width - 220f, 58f), "MENU", estiloBotao)) VoltarAoMenu();
    }

    private void DesenharFim(float largura, float altura)
    {
        DesenharSobreposicao(largura, altura);
        Rect painel = RetanguloCentral(largura, altura, 650f, 500f);
        DesenharPainel(painel);
        GUI.Label(new Rect(painel.x + 35f, painel.y + 48f, painel.width - 70f, 72f), "FIM DE JOGO", estiloTituloPainel);
        GUI.Label(new Rect(painel.x + 50f, painel.y + 145f, painel.width - 100f, 72f), "Pontuação  " + pontuacao.ToString("D4") + "\nRecorde  " + recorde.ToString("D4"), estiloCorpo);
        if (GUI.Button(new Rect(painel.x + 130f, painel.y + 268f, painel.width - 260f, 62f), "TENTAR NOVAMENTE", estiloBotao)) ComecarPartida();
        if (GUI.Button(new Rect(painel.x + 130f, painel.y + 354f, painel.width - 260f, 62f), "MENU", estiloBotao)) VoltarAoMenu();
    }

    private void DesenharVitoria(float largura, float altura)
    {
        DesenharSobreposicao(largura, altura);
        Rect painel = RetanguloCentral(largura, altura, 650f, 500f);
        DesenharPainel(painel);
        GUI.Label(new Rect(painel.x + 35f, painel.y + 48f, painel.width - 70f, 72f), "MASMORRA CONCLUÍDA", estiloTituloPainel);
        GUI.Label(new Rect(painel.x + 50f, painel.y + 145f, painel.width - 100f, 72f), "Você ocupou toda a arena.\nPontuação  " + pontuacao.ToString("D4"), estiloCorpo);
        if (GUI.Button(new Rect(painel.x + 130f, painel.y + 268f, painel.width - 260f, 62f), "JOGAR NOVAMENTE", estiloBotao)) ComecarPartida();
        if (GUI.Button(new Rect(painel.x + 130f, painel.y + 354f, painel.width - 260f, 62f), "MENU", estiloBotao)) VoltarAoMenu();
    }

    private Rect RetanguloCentral(float largura, float altura, float w, float h)
    {
        return new Rect((largura - w) * 0.5f, (altura - h) * 0.5f, w, h);
    }

    private void DesenharSobreposicao(float largura, float altura)
    {
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0f, 0f, largura, altura), texturaSobreposicao);
    }

    private void DesenharPainel(Rect retangulo)
    {
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(retangulo.x - 3f, retangulo.y - 3f, retangulo.width + 6f, retangulo.height + 6f), texturaBorda);
        GUI.DrawTexture(retangulo, texturaPainel);
    }

    private Texture2D CriarTexturaPiso()
    {
        int largura = LarguraGrade * PixelsPorCelula;
        int altura = AlturaGrade * PixelsPorCelula;
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[largura * altura];

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                int localX = x % PixelsPorCelula;
                int localY = y % PixelsPorCelula;
                int celulaX = x / PixelsPorCelula;
                int celulaY = y / PixelsPorCelula;
                int variacao = (celulaX * 17 + celulaY * 29 + celulaX * celulaY * 7) % 13;
                Color32 cor;
                if (localX < 2 || localY < 2)
                    cor = new Color32(24, 33, 36, 255);
                else if (localX == 2 || localY == 2)
                    cor = new Color32(62, 72, 72, 255);
                else
                {
                    byte baseR = (byte)(43 + variacao);
                    byte baseG = (byte)(53 + variacao);
                    byte baseB = (byte)(53 + variacao / 2);
                    if (localX < 5 || localY < 5)
                        cor = new Color32((byte)(baseR + 8), (byte)(baseG + 8), (byte)(baseB + 7), 255);
                    else
                        cor = new Color32(baseR, baseG, baseB, 255);
                    int marca = (x * 73856093) ^ (y * 19349663);
                    if ((marca & 255) == 0)
                        cor = new Color32((byte)(baseR + 13), (byte)(baseG + 12), (byte)(baseB + 9), 255);
                }
                pixels[x + y * largura] = cor;
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply(false, false);
        return textura;
    }

    private Texture2D CriarTexturaCorpo(bool cabeca)
    {
        const int tamanho = 48;
        Texture2D textura = NovaTextura(tamanho, tamanho);
        Color32[] pixels = new Color32[tamanho * tamanho];
        Color32 transparente = new Color32(0, 0, 0, 0);
        Color32 contorno = new Color32(12, 42, 31, 255);
        Color32 corpo = new Color32(49, 154, 91, 255);
        Color32 brilho = new Color32(111, 207, 126, 255);
        Color32 olho = new Color32(245, 235, 200, 255);
        Color32 pupila = new Color32(20, 32, 27, 255);

        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                if (DentroRetanguloArredondado(x, y, tamanho, 5))
                {
                    bool borda = !DentroRetanguloArredondado(x, y, tamanho, 8);
                    Color32 cor = borda ? contorno : corpo;
                    if (!borda && y > 30 && x > 11 && x < 37)
                        cor = brilho;
                    if (!cabeca && ((x > 14 && x < 18 && y > 13 && y < 17) || (x > 28 && x < 32 && y > 29 && y < 33)))
                        cor = new Color32(157, 225, 145, 255);
                    if (cabeca && DentroOlho(x, y, 32, 31)) cor = olho;
                    if (cabeca && DentroOlho(x, y, 32, 17)) cor = olho;
                    if (cabeca && DentroOlho(x, y, 35, 31)) cor = pupila;
                    if (cabeca && DentroOlho(x, y, 35, 17)) cor = pupila;
                    pixels[x + y * tamanho] = cor;
                }
                else pixels[x + y * tamanho] = transparente;
            }
        }
        textura.SetPixels32(pixels);
        textura.Apply(false, false);
        return textura;
    }

    private bool DentroRetanguloArredondado(int x, int y, int tamanho, int raio)
    {
        int cx = Mathf.Clamp(x, raio, tamanho - raio - 1);
        int cy = Mathf.Clamp(y, raio, tamanho - raio - 1);
        int dx = x - cx;
        int dy = y - cy;
        return dx * dx + dy * dy <= raio * raio;
    }

    private bool DentroOlho(int x, int y, int cx, int cy)
    {
        int dx = x - cx;
        int dy = y - cy;
        return dx * dx + dy * dy <= 10;
    }

    private Texture2D CriarTexturaMaca()
    {
        const int tamanho = 48;
        Texture2D textura = NovaTextura(tamanho, tamanho);
        Color32[] pixels = new Color32[tamanho * tamanho];
        Color32 transparente = new Color32(0, 0, 0, 0);
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                Color32 cor = transparente;
                float dx = (x - 23.5f) / 16f;
                float dy = (y - 21f) / 16f;
                float d = dx * dx + dy * dy;
                if (d < 1.02f && y < 35)
                {
                    cor = d > 0.83f ? new Color32(94, 30, 27, 255) : new Color32(204, 61, 43, 255);
                    if (x < 20 && y > 24) cor = new Color32(241, 120, 75, 255);
                }
                if (x >= 23 && x <= 26 && y >= 34 && y <= 42)
                    cor = new Color32(109, 69, 37, 255);
                if (x >= 26 && x <= 35 && y >= 37 && y <= 41 && (x + y) % 3 != 0)
                    cor = new Color32(104, 174, 78, 255);
                pixels[x + y * tamanho] = cor;
            }
        }
        textura.SetPixels32(pixels);
        textura.Apply(false, false);
        return textura;
    }

    private Texture2D CriarTexturaBrilho()
    {
        const int tamanho = 64;
        Texture2D textura = NovaTextura(tamanho, tamanho);
        Color32[] pixels = new Color32[tamanho * tamanho];
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float dx = (x - 31.5f) / 31.5f;
                float dy = (y - 31.5f) / 31.5f;
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                byte alpha = (byte)(Mathf.Clamp01(1f - distancia) * 170f);
                pixels[x + y * tamanho] = new Color32(255, 169, 74, alpha);
            }
        }
        textura.SetPixels32(pixels);
        textura.Apply(false, false);
        return textura;
    }

    private Texture2D CriarTexturaSolida(Color32 cor)
    {
        Texture2D textura = NovaTextura(1, 1);
        textura.SetPixels32(new Color32[] { cor });
        textura.Apply(false, false);
        return textura;
    }

    private Texture2D NovaTextura(int largura, int altura)
    {
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        return textura;
    }

    private Sprite CriarSprite(Texture2D textura, float pixelsPorUnidade)
    {
        return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), pixelsPorUnidade);
    }
}
