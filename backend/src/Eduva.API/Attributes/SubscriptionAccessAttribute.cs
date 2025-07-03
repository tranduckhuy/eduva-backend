using Eduva.Domain.Enums;

namespace Eduva.API.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SubscriptionAccessAttribute : Attribute
    {
        public SubscriptionAccessLevel Level { get; }

        public SubscriptionAccessAttribute(SubscriptionAccessLevel level)
        {
            Level = level;
        }

    }
}
