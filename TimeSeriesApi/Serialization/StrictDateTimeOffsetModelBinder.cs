using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TimeSeriesApi.Serialization
{
    /// <summary>
    /// Binds DateTimeOffset? query parameters, requiring "Z" or an offset.
    /// The default binder cannot tell "2026-11-13T00:00:00" from "2026-11-13T00:00:00Z" after parsing,
    /// so the raw string has to be checked before parsing.
    /// </summary>
    public class StrictDateTimeOffsetModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (result == ValueProviderResult.None)
            {
                return Task.CompletedTask; // Parameter not supplied: leave it null.
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);

            var text = result.FirstValue;
            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.CompletedTask;
            }

            if (StrictTimestamp.TryParse(text, out var value))
            {
                bindingContext.Result = ModelBindingResult.Success(value);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, StrictTimestamp.ErrorMessage);
            }

            return Task.CompletedTask;
        }
    }
}