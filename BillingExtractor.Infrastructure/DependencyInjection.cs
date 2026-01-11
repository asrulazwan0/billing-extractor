using BillingExtractor.Application.Interfaces;
using BillingExtractor.Infrastructure.LLM;
using BillingExtractor.Infrastructure.Persistence;
using BillingExtractor.Infrastructure.Repositories;
using BillingExtractor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingExtractor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        // Services
        services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();

        // LLM Services - Register based on configuration
        var llmProvider = configuration.GetValue<string>("LLM:Provider", "Gemini");
        
        if (llmProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<OpenAIService>();
            services.Configure<OpenAIOptions>(configuration.GetSection("LLM:OpenAI"));
            services.AddScoped<IInvoiceExtractor, OpenAIService>();
        }
        else
        {
            // Default to Gemini
            services.Configure<GeminiOptions>(configuration.GetSection("LLM:Gemini"));
            services.AddScoped<IInvoiceExtractor, GeminiService>();
        }

        // File Storage
        services.Configure<LocalFileStorageOptions>(configuration.GetSection("FileStorage"));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}