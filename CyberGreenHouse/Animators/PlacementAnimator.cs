using Avalonia.Animation;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.Animators
{
    public class PlacementAnimator : InterpolatingAnimator<PlacementMode>
    {
        public override PlacementMode Interpolate(double progress, PlacementMode oldValue, PlacementMode newValue)
        {
            return PlacementMode.Center;
        }
    }
}
