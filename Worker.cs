using Quartz;
using DietWorker.Services;

namespace DietWorker.Jobs
{
    public class DailyRecommendationJob : IJob
    {
        private readonly IDietService _dietService;

        public DailyRecommendationJob(IDietService dietService)
        {
            _dietService = dietService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // Qui chiami la logica principale
            await _dietService.RunDailyRecommendationAsync();
        }
    }
}