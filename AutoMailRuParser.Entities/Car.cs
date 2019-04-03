using System;
using System.Collections.Generic;

namespace AutoMailRuParser.Entities
{
    /// <summary>
    /// Класс инкапсулирующий информацию по машине (по модификации модели машины)
    /// </summary>
    public class Car
    {
        public Dictionary<string, string> EngineSpec { get; set; }

        public Dictionary<string, string> DynamicSpec { get; set; }

        public Dictionary<string, string> TransmissionSpec { get; set; }

        public Dictionary<string, string> ChassisSpec { get; set; }

        public Dictionary<string, string> BodySpec { get; set; }

        public Dictionary<string, string> SteeringSpec { get; set; }

        public Dictionary<string, string> DimensionsSpec { get; set; }

        public Dictionary<string, string> OtherSpec { get; set; }

        public string Brand { get; set; }

        public string Model { get; set; }

        public string ProductionYears { get; set; }

        public string Price { get; set; }

        public string Modification { get; set; }

        public string Description { get; set; }

        public override string ToString()
        {
            return $"{this.Brand} {this.Model} {this.Modification}";
        }
    }
}

