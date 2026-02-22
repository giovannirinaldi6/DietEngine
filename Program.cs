using Microsoft.EntityFrameworkCore;
using DietWorker.Models;
using DietWorker.Services;
using Quartz;
using DietWorker.Jobs;
using OpenAI.Chat;
using Microsoft.Extensions.DependencyInjection;
using DietWorker.DTO;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // DbContext
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(hostContext.Configuration.GetConnectionString("DefaultConnection")));

    // DietService
    services.AddScoped<IDietService, DietService>();

    services.AddSingleton<ChatClient>(serviceProvider =>
    {
        var apiKey = hostContext.Configuration["OpenAI:ApiKey"];
        var model = "gpt-4o";

        return new ChatClient(model, apiKey);
    });

    services.Configure<EmailServiceOptions>(hostContext.Configuration.GetSection("EmailService"));
    services.Configure<PersoneOptions>(hostContext.Configuration.GetSection("Persone"));

    // Registra EmailService come singleton usando le opzioni
    services.AddSingleton<EmailService>();

    // Quartz
    services.AddQuartz(q =>
    {
        // Job e Trigger
        var jobKey = new JobKey("DailyRecommendationJob");
        q.AddJob<DailyRecommendationJob>(opts => opts.WithIdentity(jobKey));

        // Trigger giornaliero {secondi} {minuti} {ore} {giorno del mese} {mese} {giorno della settimana} {anno?}
        q.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity("DailyRecommendationTrigger")
            .WithCronSchedule("0 0 11 * * ?")); // Cron: 11:00 ogni giorno
    });

    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
});

var host = builder.Build();
await host.RunAsync();