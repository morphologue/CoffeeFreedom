using System.Linq;
using CoffeeFreedom.Common.Models;

namespace CoffeeFreedom.Common.Extensions
{
    public static class OrderExtensions
    {
        private static readonly decimal[] AllowedSugarQuantities = {0.5M, 1M, 2M};
        private static readonly decimal[] AllowedProportionsFull = {0.5M, 0.75M, 1M};

        public static string Validate(this Order order)
        {
            // Sweetener and SweetenerQuantity
            switch (order.Sweetener)
            {
                case null:
                    if (order.SweetenerQuantity != 0M)
                    {
                        return "If Sweetener is null, SweetenerQuantity must be 0";
                    }
                    break;
                case Sweetener.Honey:
                    if (order.SweetenerQuantity != 1M)
                    {
                        return "CafeIT only supports quantity 1 for Honey";
                    }
                    break;
                case Sweetener.Equal:
                    if (order.SweetenerQuantity != 2M)
                    {
                        return "CafeIT only supports quantity 2 for Equal";
                    }
                    break;
                case Sweetener.Sugar:
                    if (!AllowedSugarQuantities.Contains(order.SweetenerQuantity))
                    {
                        return $"CafeIT only supports quantities {string.Join(", ", AllowedSugarQuantities)} for Sugar. Set SweetenerQuantity to null if you don't want any sweetener";
                    }
                    break;
            }

            if (!AllowedProportionsFull.Contains(order.ProportionFull))
            {
                return $"CafeIT only supports fill proportions {string.Join(", ", AllowedProportionsFull)}";
            }

            // Null and collections don't mix. Let's just fix this silently.
            if (order.Customisations == null)
            {
                order.Customisations = new Customisation[0];
            }

            if (order.Customisations.Length != order.Customisations.Distinct().Count())
            {
                return "Duplicate customisations are not allowed";
            }

            // Go above and beyond.
            if (order.Customisations.Contains(Customisation.Weak) && order.Customisations.Contains(Customisation.Strong))
            {
                return "Although allowed by CafeIT, one wonders how a coffee could be simultaneously weak and strong";
            }
            if (order.Customisations.Contains(Customisation.ExtraHot) && order.Customisations.Contains(Customisation.Warm))
            {
                return "Although allowed by CafeIT, one wonders how a coffee could be simultaneously extra hot and warm";
            }

            return null;
        }
    }
}
