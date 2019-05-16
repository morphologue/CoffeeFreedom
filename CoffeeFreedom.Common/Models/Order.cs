using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoffeeFreedom.Common.Models
{
    public class Order
    {
        public Size Size { get; set; }
        public Milk? Milk { get; set; }
        public Dash? Dash { get; set; }
        public Sweetener? Sweetener { get; set; }
        public float SweetenerQuantity { get; set; }
        public float ProportionFull { get; set; }
        public Customisation[] Customisations { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Size
    {
        MiniCup,
        Small,
        Large,
        KeepCup
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Milk
    {
        FullCream,
        Skim,
        Soy,
        Almond
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Dash
    {
        ColdMilk,
        HotMilk,
        ColdWater,
        HotWater
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Sweetener
    {
        Sugar,
        Equal,
        Honey
    }

    [JsonConverter(typeof(StringEnumConverter))]
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
