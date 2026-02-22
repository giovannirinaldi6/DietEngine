namespace DietWorker.Services
{
    public interface IDietService
    {
        Task<bool> RunDailyRecommendationAsync();
    }
}
