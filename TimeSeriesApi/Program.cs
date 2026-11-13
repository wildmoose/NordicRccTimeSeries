using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using TimeSeriesApi.Data;
using TimeSeriesApi.Services;

namespace TimeSeriesApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    // Enums as "DK1" / "Production" instead of 0 / 1 in both requests and responses.
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // RFC 9457 problem details for all error responses (400, 404, 500 ...).
            builder.Services.AddProblemDetails();
            builder.Services.AddOpenApi();

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ITimeSeriesRepository, InMemoryTimeSeriesRepository>();
            builder.Services.AddSingleton<ITimeSeriesValidator, TimeSeriesValidator>();

            var app = builder.Build();

            // Unhandled exceptions become a 500 problem details response without leaking stack traces.
            app.UseExceptionHandler();
            app.UseStatusCodePages();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.MapControllers();

            app.Run();
        }
    }
}