using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaterMod;

namespace TTQMM_WeatherMod
{
    public class WaterMod
    {
        public static float WaterHeight
        {
            get => QPatch.WaterHeight;
        }
    }
}
