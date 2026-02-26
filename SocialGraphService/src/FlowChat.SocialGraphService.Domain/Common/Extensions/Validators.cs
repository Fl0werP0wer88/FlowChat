using System.Runtime.CompilerServices;

namespace FlowChat.SocialGraphService.Domain.Common.Extensions;

public static class Validators
{
        public static T EnsureNotDefault<T>(this T model, [CallerArgumentExpression("model")] string name = "") where T : struct
        {
            if (model.Equals(default(T)))
                throw new ArgumentException($"{name} cannot be null or default.");
            return model;
        }
}
