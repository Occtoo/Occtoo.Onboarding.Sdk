using System.Collections.Generic;

namespace Occtoo.Onboarding.Sdk.Models
{
    public class DynamicEntity
    {
        public string Key { get; set; }

        public bool Delete { get; set; }

        public List<DynamicProperty> Properties { get; set; } = new List<DynamicProperty>();

        public override string ToString() => string.IsNullOrEmpty(Key) ? base.ToString() : Key;
    }

    public class DynamicProperty
    {
        public string Id { get; set; }

        public string Value { get; set; }

        public string Language { get; set; }

        public override string ToString() => string.IsNullOrEmpty(Id) ? base.ToString() : Id;
    }
}