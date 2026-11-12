using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Controla el shader Demo/Holograma desde campos simples que el Timeline
/// puede animar (Reveal, Glitch, Intensidad). También sube el peso del
/// Volume local (Chromatic Aberration + Film Grain) cuando hay glitch,
/// así el post-procesado queda sincronizado con el efecto.
///
/// Va en un objeto vacío "HologramaFX" con su propio Animator; el Timeline
/// anima estos campos con un Animation Track ligado a ese Animator.
/// </summary>
[ExecuteAlways]
public class HologramaFX : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Renderers del personaje holograma (SkinnedMeshRenderer).")]
    public Renderer[] renderers;

    [Tooltip("Transform del personaje: su Y se usa como base de la aparición.")]
    public Transform personaje;

    [Tooltip("Volume local con Chromatic Aberration + Film Grain (opcional).")]
    public Volume volumenPost;

    [Header("Animables desde el Timeline")]
    [Range(0f, 1.1f)] public float reveal = 1.1f;
    [Range(0f, 1f)]   public float glitch = 0f;
    [Range(0f, 2f)]   public float intensidad = 1f;

    [Header("Ajustes")]
    [Tooltip("Altura aproximada del personaje en metros.")]
    public float altura = 1.9f;
    [Tooltip("Glitch mínimo aunque 'glitch' esté en 0 (se ve más vivo).")]
    [Range(0f, 0.3f)] public float glitchBase = 0.04f;
    [Tooltip("Peso mínimo del Volume mientras el holograma está visible.")]
    [Range(0f, 1f)] public float pesoPostBase = 0.25f;

    static readonly int ID_Reveal      = Shader.PropertyToID("_Reveal");
    static readonly int ID_MinY        = Shader.PropertyToID("_RevealMinY");
    static readonly int ID_MaxY        = Shader.PropertyToID("_RevealMaxY");
    static readonly int ID_GlitchAmt   = Shader.PropertyToID("_GlitchAmount");
    static readonly int ID_GlitchProb  = Shader.PropertyToID("_GlitchProbability");
    static readonly int ID_Intensity   = Shader.PropertyToID("_Intensity");

    MaterialPropertyBlock _mpb;

    void LateUpdate()
    {
        if (renderers == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        float baseY = personaje != null ? personaje.position.y : transform.position.y;
        float g = Mathf.Clamp01(glitchBase + glitch);

        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ID_Reveal, reveal);
            _mpb.SetFloat(ID_MinY, baseY);
            _mpb.SetFloat(ID_MaxY, baseY + altura);
            _mpb.SetFloat(ID_GlitchAmt, Mathf.Lerp(0.01f, 0.18f, g));
            _mpb.SetFloat(ID_GlitchProb, Mathf.Lerp(0.02f, 0.55f, g));
            _mpb.SetFloat(ID_Intensity, intensidad);
            r.SetPropertyBlock(_mpb);
        }

        if (volumenPost != null)
        {
            float visible = Mathf.Clamp01(reveal) * Mathf.Clamp01(intensidad);
            volumenPost.weight = Mathf.Clamp01(visible * pesoPostBase + glitch);
        }
    }
}
