using System;

namespace Entities
{
    public class InputSuggestion
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public virtual string Id { get; set; }
        public Profiili Profiili { get; set; }
        public string Label { get; set; }
        public string Suggestion { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public int Likes { get; set; }
    }
}
