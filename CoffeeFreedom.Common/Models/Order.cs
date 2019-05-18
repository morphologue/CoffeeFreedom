using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoffeeFreedom.Common.Models
{
    public class Order
    {
        public Variant Variant { get; set; }
        public Size Size { get; set; }
        public Milk? Milk { get; set; }
        public Dash? Dash { get; set; }
        public Sweetener? Sweetener { get; set; }
        public decimal SweetenerQuantity { get; set; }
        public decimal ProportionFull { get; set; }
        public Customisation[] Customisations { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Variant
    {
        Espresso,
        DoubleEspresso,
        DirtyChai,
        ShortMacchiato,
        HotChocolate,
        LongMacchiato,
        LongBlack,
        Latte,
        Cappuccino,
        FlatWhite,
        Piccolo,
        Mocha,
        IcedCoffee,
        Chai
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
