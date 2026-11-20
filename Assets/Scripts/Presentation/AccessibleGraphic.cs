using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Los colores de acento cambian según el modo elegido, no es un filtro que simule el daltonismo.</summary>
    public sealed class AccessibleGraphic : MonoBehaviour
    {
        /// <summary>El gráfico que cambia de color, asignado en la escena o el prefab.</summary>
        public Graphic target;

        /// <summary>El color original sirve de base para que los cambios de paleta no se acumulen.</summary>
        public Color normalColor = Color.white;

        /// <summary>Color de la paleta, de cero a cuatro, con menos uno se elige según el tono original.</summary>
        public int paletteIndex = -1;

        /// <summary>El modo protanopia lleva azul, naranja, violeta, cian y rosa.</summary>
        static readonly Color[] ProtanopiaPalette =
        {
            new(0.15f, 0.48f, 0.95f), new(1f, 0.65f, 0.10f),
            new(0.73f, 0.42f, 0.95f), new(0.10f, 0.80f, 0.88f),
            new(0.95f, 0.42f, 0.68f)
        };

        /// <summary>El modo deuteranopia lleva azul, amarillo, magenta, naranja y cian.</summary>
        static readonly Color[] DeuteranopiaPalette =
        {
            new(0.15f, 0.48f, 0.95f), new(0.95f, 0.82f, 0.15f),
            new(0.85f, 0.40f, 0.80f), new(1f, 0.58f, 0.12f),
            new(0.12f, 0.80f, 0.88f)
        };

        /// <summary>El modo tritanopia lleva rojo, cian, violeta, azul grisáceo y salmón.</summary>
        static readonly Color[] TritanopiaPalette =
        {
            new(0.95f, 0.35f, 0.40f), new(0.12f, 0.80f, 0.84f),
            new(0.78f, 0.40f, 0.78f), new(0.55f, 0.76f, 0.84f),
            new(1f, 0.58f, 0.50f)
        };

        /// <summary>El gráfico escucha los cambios de opciones mientras está activo.</summary>
        void OnEnable()
        {
            GamePreferences.Changed += Refresh;
            Refresh();
        }

        /// <summary>Al desactivar el gráfico, se deja de escuchar cambios de opciones.</summary>
        void OnDisable()
        {
            GamePreferences.Changed -= Refresh;
        }

        /// <summary>Se aplica la paleta elegida sin perder la luminosidad ni la transparencia del diseño.</summary>
        public void Refresh()
        {
            if (target != null)
                target.color = ResolveColor(GamePreferences.ColorMode);
        }

        /// <summary>Se usa el acento asignado o, en modo automático, se elige según el tono del diseño.</summary>
        /// <param name="mode">Paleta elegida en las preferencias, con cero para el color original.</param>
        Color ResolveColor(int mode)
        {
            if (mode == 0)
                return normalColor;

            Color[] palette = mode switch
            {
                1 => ProtanopiaPalette,
                2 => DeuteranopiaPalette,
                _ => TritanopiaPalette
            };

            int accentIndex = paletteIndex;

            if (accentIndex < 0)
            {
                Color.RGBToHSV(normalColor, out float hue, out float saturation, out _);

                if (saturation < 0.15f)
                    return normalColor;

                accentIndex = Mathf.FloorToInt(hue * palette.Length);
            }

            Color color = MatchLuminance(palette[Mathf.Clamp(accentIndex, 0, palette.Length - 1)], normalColor);
            color.a = normalColor.a;
            return color;
        }

        /// <summary>Se ajusta la luminosidad del acento para mantener el contraste del diseño original.</summary>
        static Color MatchLuminance(Color accent, Color source)
        {
            Color linearAccent = accent.linear;
            float sourceLuminance = Luminance(source.linear);
            float accentLuminance = Luminance(linearAccent);

            // Si sobra luz, el color se oscurece, si falta, se acerca al blanco.
            if (sourceLuminance <= accentLuminance)
                linearAccent *= sourceLuminance / Mathf.Max(0.0001f, accentLuminance);
            else
                linearAccent = Color.Lerp(linearAccent, Color.white,
                    (sourceLuminance - accentLuminance) / Mathf.Max(0.0001f, 1f - accentLuminance));

            return linearAccent.gamma;
        }

        /// <summary>La luminosidad se calcula en espacio lineal, cada canal tiene su peso en la fórmula.</summary>
        static float Luminance(Color linearColor)
        {
            return 0.2126f * linearColor.r + 0.7152f * linearColor.g + 0.0722f * linearColor.b;
        }
    }
}
