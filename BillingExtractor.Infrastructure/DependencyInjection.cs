using BillingExtractor.Application.Interfaces;
using BillingExtractor.Infrastructure.FileStorage;
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
        // Database - Choose provider based on connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (connectionString != null && connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        // Repositories
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        // Services
        services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();
        services.AddScoped<IInvoiceValidator, InvoiceValidator>();
        services.AddScoped<IFileProcessor, FileProcessor>();

        // LLM Services - Register based on configuration
        var llmProvider = configuration["LLM:Provider"] ?? "Gemini";

        if (llmProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IInvoiceExtractor, MockLLMService>();
        }
        else if (llmProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<OpenAIService>();
            services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
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