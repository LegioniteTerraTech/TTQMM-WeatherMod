using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTQMM_WeatherMod
{
    public class WaterModIntegration
    {
        public static float WaterHeight
        {
            get => WaterMod.QPatch.WaterHeight;
        }
    }
}
