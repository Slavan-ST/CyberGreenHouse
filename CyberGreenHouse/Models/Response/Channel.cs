using System.Text.Json.Serialization;
using System;

namespace CyberGreenHouse.Models.Response
{
    public class Channel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "undefined";
        public string Latitude { get; set; } = "undefined";
        public string Longitude { get; set; } = "undefined";
        public string Field1 { get; set; } = "undefined"; // Название поля 1 (Temp)
        public string Field2 { get; set; } = "undefined"; // Название поля 2 (Humd)
        public string Field3 { get; set; } = "undefined"; // Название поля 3 (Humd почва)

        [JsonPropertyName("created_at")]
        public string CreatedAtString { get; set; } = "undefined";

        [JsonPropertyName("updated_at")]
        public string UpdatedAtString { get; set; } = "undefined";

        public int LastEntryId { get; set; }

        [JsonIgnore]
        public DateTime CreatedAt => DateTime.Parse(CreatedAtString);

        [JsonIgnore]
        public DateTime UpdatedAt => DateTime.Parse(UpdatedAtString);

        public override string ToString()
        {
            return Name;
        }
    }
}