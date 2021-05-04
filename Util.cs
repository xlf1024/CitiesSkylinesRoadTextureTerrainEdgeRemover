using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Globalization;
using UnityEngine;

namespace RoadTextureTerrainEdgeRemover
{
    class Util
    {
        public static Byte Clamp(Byte value, Byte min, Byte max)
        {
            if (value < min) return min;
            else if (value > max) return max;
            else return value;
        }
        public static int LenientStringToInt(string s, int min, int max, int fallback)
        {
            IFormatProvider invariantProvider = CultureInfo.InvariantCulture;
            NumberStyles style = NumberStyles.Any;
            if (Double.TryParse(s.Replace(',', '.'), style, invariantProvider, out double x))
            {
                return Mathf.Clamp(Mathf.RoundToInt((float)x), min, max);
            }
            else
            {
                foreach(IFormatProvider provider in new IFormatProvider[]{CultureInfo.InvariantCulture, CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture,
    CultureInfo.InstalledUICulture })
                {
                    if(Double.TryParse(s, style, provider, out x))
                    {
                        return Mathf.Clamp(Mathf.RoundToInt((float)x), min, max);
                    }
                }
                return fallback;
            }
        }
    }
}
