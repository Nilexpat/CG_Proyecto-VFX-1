using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// Controla la demo:
///  - Elegir efecto (solo uno activo a la vez; todos en el mismo punto, así parece que el personaje cambia de efecto).
///  - Play / Pausa del efecto elegido (Time.timeScale: pausa animación, partículas, Timeline y shaders).
///  - Reiniciar el efecto.
///  - Slider de tono: cambia el color de los shaders de los efectos en tiempo real.
///    Trabaja sobre COPIAS de los materiales, así que al cerrar y abrir la app vuelve al color original
///    y los materiales del proyecto nunca se modifican.
/// </summary>
public class EffectManager : MonoBehaviour
{
    [System.Serializable]
    public class Efecto
    {
        public string nombre;
        public GameObject raiz;
        public PlayableDirector director;
        public Button boton;
    }

    [Header("Efectos")]
    public Efecto[] efectos;

    [Header("UI")]
    public Button botonPlayPausa;
    public Text textoPlayPausa;
    public Button botonReiniciar;
    public Slider sliderTono;
    public Text textoTono;
    public Image manijaTono;

    [Header("Estilo")]
    public Color colorBoton = new Color(0.12f, 0.14f, 0.16f, 0.9f);
    public Color colorBotonActivo = new Color(0.1f, 0.65f, 0.62f, 1f);

    // Propiedades de color de los Shader Graph de la demo
    static readonly string[] PropsColor = { "_Color", "_CoreColor", "_EdgeColor", "_HotColor", "_GlitchColor" };

    struct ColorGuardado { public Material mat; public int id; public Color original; }
    struct LuzGuardada { public Light luz; public Color original; }

    readonly List<ColorGuardado> _colores = new List<ColorGuardado>();
    readonly List<LuzGuardada> _luces = new List<LuzGuardada>();
    int _actual = -1;
    bool _pausado;

    void Start()
    {
        CopiarMateriales();

        for (int i = 0; i < efectos.Length; i++)
        {
            int idx = i;
            if (efectos[i].boton != null) efectos[i].boton.onClick.AddListener(() => Seleccionar(idx));
            if (efectos[i].director != null) efectos[i].director.playOnAwake = false;
        }
        if (botonPlayPausa != null) botonPlayPausa.onClick.AddListener(PlayPausa);
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(Reiniciar);
        if (sliderTono != null)
        {
            sliderTono.minValue = 0f; sliderTono.maxValue = 1f; sliderTono.value = 0f;
            sliderTono.onValueChanged.AddListener(CambiarTono);
        }

        Seleccionar(0);
        CambiarTono(0f);
    }

    // ------------------------------------------------------------------
    public void Seleccionar(int i)
    {
        if (efectos == null || efectos.Length == 0) return;
        _actual = Mathf.Clamp(i, 0, efectos.Length - 1);
        _pausado = false;
        Time.timeScale = 1f;

        for (int j = 0; j < efectos.Length; j++)
        {
            var e = efectos[j];
            bool activo = j == _actual;
            if (e.director != null && !activo) e.director.Stop();
            if (e.raiz != null) e.raiz.SetActive(activo);
            if (activo && e.director != null)
            {
                e.director.time = 0;
                e.director.Evaluate();
                e.director.Play();
            }
            if (e.boton != null) e.boton.image.color = activo ? colorBotonActivo : colorBoton;
        }
        ActualizarTextoPlay();
    }

    public void PlayPausa()
    {
        _pausado = !_pausado;
        Time.timeScale = _pausado ? 0f : 1f;
        ActualizarTextoPlay();
    }

    public void Reiniciar() => Seleccionar(_actual);

    void ActualizarTextoPlay()
    {
        if (textoPlayPausa != null) textoPlayPausa.text = _pausado ? "Reanudar" : "Pausa";
    }

    void OnDisable() => Time.timeScale = 1f;

    // ------------------------------------------------------------------
    //  Tono de los efectos
    // ------------------------------------------------------------------
    void CopiarMateriales()
    {
        var copias = new Dictionary<Material, Material>();
        foreach (var e in efectos)
        {
            if (e.raiz == null) continue;

            foreach (var r in e.raiz.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool cambio = false;
                for (int k = 0; k < mats.Length; k++)
                {
                    var m = mats[k];
                    if (m == null || !EsDeEfecto(m)) continue;
                    if (!copias.TryGetValue(m, out var copia))
                    {
                        copia = new Material(m) { name = m.name + " (runtime)" };
                        copias[m] = copia;
                        GuardarColores(copia);
                    }
                    mats[k] = copia;
                    cambio = true;
                }
                if (cambio) r.sharedMaterials = mats;
            }

            foreach (var l in e.raiz.GetComponentsInChildren<Light>(true))
                _luces.Add(new LuzGuardada { luz = l, original = l.color });
        }
    }

    static bool EsDeEfecto(Material m)
    {
        string s = m.shader != null ? m.shader.name : "";
        return s.Contains("SH_VFX") || s.Contains("Particles");
    }

    void GuardarColores(Material m)
    {
        foreach (var p in PropsColor)
        {
            if (!m.HasProperty(p)) continue;
            int id = Shader.PropertyToID(p);
            _colores.Add(new ColorGuardado { mat = m, id = id, original = m.GetColor(id) });
        }
        if (m.shader.name.Contains("Particles") && m.HasProperty("_BaseColor"))
        {
            int id = Shader.PropertyToID("_BaseColor");
            _colores.Add(new ColorGuardado { mat = m, id = id, original = m.GetColor(id) });
        }
    }

    public void CambiarTono(float desplazamiento)
    {
        foreach (var c in _colores)
            c.mat.SetColor(c.id, Rotar(c.original, desplazamiento));
        foreach (var l in _luces)
            if (l.luz != null) l.luz.color = Rotar(l.original, desplazamiento);

        if (textoTono != null) textoTono.text = $"Tono de los efectos  {Mathf.RoundToInt(desplazamiento * 360f)}°";
        if (manijaTono != null) manijaTono.color = Rotar(new Color(0.3f, 1f, 0.95f), desplazamiento);
    }

    /// Gira el tono de un color (también HDR) conservando su brillo.
    static Color Rotar(Color c, float desplazamiento)
    {
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        if (max <= 0.0001f) return c;
        var baseC = new Color(c.r / max, c.g / max, c.b / max);
        Color.RGBToHSV(baseC, out float h, out float s, out float v);
        h = Mathf.Repeat(h + desplazamiento, 1f);
        var r = Color.HSVToRGB(h, s, v) * max;
        r.a = c.a;
        return r;
    }
}
