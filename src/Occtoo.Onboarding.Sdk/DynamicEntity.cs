using System.Collections.Generic;

namespace Occtoo.Onboarding.Sdk
{
    public class DynamicEntity
    {
        public string Key { get; set; }

        public bool Delete { get; set; }

        public List<DynamicProperty> Properties { get; set; } = new List<DynamicProperty>();

        public override string ToString() => string.IsNullOrEmpty(this.Key) ? base.ToString() : this.Key;
    }

    public class DynamicProperty
    {
        public string Id { get; set; }

        public string Value { get; set; }

        public string Language { get; set; }

        public override string ToString() => string.IsNullOrEmpty(this.Id) ? base.ToString() : this.Id;
    }
}