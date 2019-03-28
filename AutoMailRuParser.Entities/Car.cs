using System;
using System.Collections.Generic;

namespace AutoMailRuParser.Entities
{
    public class Car
    {
        public Car()
        {
            this.EngineSpec = new Dictionary<string, string>();
            this.DynamicSpec = new Dictionary<string, string>();
            this.TransmissionSpec = new Dictionary<string, string>();
            this.ChassisSpec = new Dictionary<string, string>();
            this.BodySpec = new Dictionary<string, string>();
            this.SteeringSpec = new Dictionary<string, string>();
            this.DimensionsSpec = new Dictionary<string, string>();
            this.OtherSpec = new Dictionary<string, string>();
            this.Brand = string.Empty;
            this.Model = string.Empty;
            this.ProductionYears = string.Empty;
            this.Price = string.Empty;
            this.Modification = string.Empty;
            this.Description = string.Empty;
        }

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

