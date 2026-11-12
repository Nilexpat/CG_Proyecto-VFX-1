using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Aura Super Saiyajin del Power Up. Va en el objeto "AuraFX", que ES el Particle System de las llamas
/// (las Chispas son un sistema hijo), así se puede controlar con una Control Track.
///
/// Valores que anima el Timeline:
///  - intensidad: cuántas llamas salen y qué tan brillantes (estalla ~4 en el clímax, remanente ~0.4)
///  - altura:     velocidad con la que suben (remanente alto: vuelan muy arriba)
///  - tamano:     tamaño de las llamas (remanente: chiquitas)
///  - rapidez:    velocidad de simulación (remanente: se desvanecen rápido)
///  - borde:      brillo del borde del personaje (Shader Graph SH_VFX_AuraBorde)
/// </summary>
[ExecuteAlways]
public class AuraFX : MonoBehaviour
{
    [Header("Referencias")]
    public ParticleSystem llamas;
    public ParticleSystem chispas;
    public Volume volumenPost;
    [Tooltip("SkinnedMeshRenderer del personaje (tiene el material de borde como segundo material).")]
    public Renderer cuerpo;

    [Header("Animables desde el Timeline")]
    [Range(0f, 5f)]     public float intensidad = 1f;
    [Range(0.05f, 3f)]  public float altura = 1f;
    [Range(0.1f, 2f)]   public float tamano = 1f;
    [Range(0.5f, 3f)]   public float rapidez = 1f;
    [Range(0f, 4f)]     public float borde = 1f;

    [Header("Ajustes")]
    public float llamasPorSegundo = 90f;
    public float chispasPorSegundo = 35f;
    public int rafagaLlamas = 80;
    public int rafagaChispas = 60;
    [Tooltip("Intensidad con la que el post-procesado llega al 100 %.")]
    public float intensidadPostMax = 3f;

    static readonly int ID_Intensity = Shader.PropertyToID("_Intensity");
    static readonly int ID_Rim       = Shader.PropertyToID("_RimIntensity");
    MaterialPropertyBlock _mpb;
    bool _rafagaHecha;

    void LateUpdate()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        if (llamas != null)
        {
            var em = llamas.emission;
            em.rateOverTimeMultiplier = intensidad * llamasPorSegundo;
            var vel = llamas.velocityOverLifetime;
            vel.speedModifierMultiplier = altura;
            var size = llamas.sizeOverLifetime;
            size.sizeMultiplier = tamano;
            var main = llamas.main;
            main.simulationSpeed = rapidez;

            var r = llamas.GetComponent<ParticleSystemRenderer>();
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ID_Intensity, 0.7f + 0.35f * intensidad);
            r.SetPropertyBlock(_mpb);
        }

        if (chispas != null)
        {
            var em = chispas.emission;
            em.rateOverTimeMultiplier = intensidad * chispasPorSegundo;
        }

        if (cuerpo != null)
        {
            cuerpo.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ID_Rim, borde);
            cuerpo.SetPropertyBlock(_mpb);
        }

        if (volumenPost != null)
            volumenPost.weight = Mathf.Clamp01(intensidad / Mathf.Max(0.01f, intensidadPostMax));

        // Ráfaga del estallido (solo en Play)
        if (Application.isPlaying)
        {
            if (!_rafagaHecha && intensidad > 3f)
            {
                if (llamas != null) llamas.Emit(rafagaLlamas);
                if (chispas != null) chispas.Emit(rafagaChispas);
                _rafagaHecha = true;
            }
            else if (intensidad < 0.2f) _rafagaHecha = false;
        }
    }
}
