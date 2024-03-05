using System.Text.Json.Serialization;

namespace RegistryV2
{
    public class Petition
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]

        public int Id { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DateTime Date { get; } = DateTime.UtcNow;

        public string Name { get; set; }
        public string PetitionText { get; set; }
        public string Email { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]

        public bool readFlag { get; set; }
    }
}