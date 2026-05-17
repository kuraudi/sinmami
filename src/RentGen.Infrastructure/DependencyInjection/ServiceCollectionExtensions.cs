using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Drafts.DTOs;
using RentGen.Infrastructure.Appendices;
using RentGen.Infrastructure.Documents;
using RentGen.Infrastructure.Drafts;
using RentGen.Infrastructure.Features;
using RentGen.Infrastructure.Guides;
using RentGen.Infrastructure.Llm.DeepSeek;
using RentGen.Infrastructure.Persistence;
using RentGen.Infrastructure.Prompts;
using RentGen.Infrastructure.Scenarios;
using RentGen.Infrastructure.Services;

namespace RentGen.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var provider = configuration["Database:Provider"];
            if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase(configuration["Database:Name"] ?? "rentgen-dev");
                return;
            }

            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.Configure<DeepSeekOptions>(configuration.GetSection(DeepSeekOptions.SectionName));
        services.AddHttpClient<IGenerativeAiClient, DeepSeekClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseProxy = false
            });

        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IFeatureAccessService, FeatureAccessService>();
        services.AddScoped<IScenarioProvider, JsonScenarioProvider>();
        services.AddScoped<IStandardGuideProvider, StandardGuideProvider>();
        services.AddScoped<IDraftService, DraftService>();
        services.AddScoped<IDraftValidationService, DraftValidationService>();
        services.AddScoped<IAiHelpService, AiHelpService>();
        services.AddScoped<IStepAnswerInterpreter, StepAnswerInterpreter>();
        services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();
        services.AddScoped<IDocumentPdfService, DocumentPdfService>();
        services.AddScoped<IAppendixService, AppendixService>();
        services.AddScoped<IAppendixPdfService, AppendixPdfService>();
        services.AddScoped<IGuideService, GuideService>();
        services.AddScoped<IGuidePdfService, GuidePdfService>();

        services.AddScoped<IPromptBuilder<Application.Prompts.RentalAgreementDocumentPromptContext>, RentalAgreementDocumentPromptBuilder>();
        services.AddScoped<IPromptBuilder<Application.Prompts.RentalAgreementGuidePromptContext>, RentalAgreementGuidePromptBuilder>();
        services.AddScoped<IPromptBuilder<Application.Prompts.RentalAgreementHelpPromptContext>, RentalAgreementHelpPromptBuilder>();
        services.AddScoped<IPromptBuilder<Application.Prompts.RentalAgreementAppendixPromptContext>, RentalAgreementAppendixPromptBuilder>();

        services.AddScoped<IValidator<CreateDraftRequest>, CreateDraftRequestValidator>();
        services.AddScoped<IValidator<SaveAnswerRequest>, SaveAnswerRequestValidator>();

        return services;
    }
}
