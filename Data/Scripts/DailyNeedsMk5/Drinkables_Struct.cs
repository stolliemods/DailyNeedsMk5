using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stollie.DailyNeeds
{
    public struct Drinkables_Struct
    {

        public float hungerRestoreValue { get; }
        public float thirstRestoreValue { get; }
        public float fatigueRestoreValue { get; }

        public Drinkables_Struct(float hungerRestoreValue, float thirstValue, float fatigueValue)
        {
            this.hungerRestoreValue = hungerRestoreValue;
            this.thirstRestoreValue = thirstValue;
            this.fatigueRestoreValue = fatigueValue;
        }
    }
}
