using System;

namespace Entities
{
    public class InputComment
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public virtual string Id { get; set; }
        public Profiili Profiili { get; set; }
        public string Comment { get; set; }
        public int Likes { get; set; }
        public string SuggestionId { get; set; }
    }
}
