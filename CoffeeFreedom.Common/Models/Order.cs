using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoffeeFreedom.Common.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public class Order
    {
        public Size Size { get; set; }
        public Milk Milk { get; set; }
        public Dash Dash { get; set; }
        public Sweetener Sweetener { get; set; }
        public float SweetenerQuantity { get; set; }
        public float ProportionFull { get; set; }
        public Customisation[] Customisations { get; set; }
    }

    public enum Size
    {
        MiniCup,
        Small,
        Large,
        KeepCup
    }

    public enum Milk
    {
        None,
        FullCream,
        Skim,
        Soy,
        Almond
    }

    public enum Dash
    {
        None,
        ColdMilk,
        HotMilk,
        ColdWater,
        HotWater
    }

    public enum Sweetener
    {
        None,
        Sugar,
        Equal,
        Honey
    }

    public enum Customisation
    {
        Caramel,
        Hazelnut,
        ExtraChocolate,
        Weak,
        Strong,
        ExtraHot,
        Warm
    }
}
