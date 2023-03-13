using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HKBalancedDifficultyMod
{
    public class GlobalSettings
    {
        public bool PreventBossScaleHp { get; set; } = true;

        public bool AutoUpdateMapOnSceneLoad { get; set; } = true;

        public bool ReduceDamage { get; set; } = true;

        public bool PreventShade { get; set; } = true;

        public bool PermanentCompass { get; set; } = true;
    }
}
