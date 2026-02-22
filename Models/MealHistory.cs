using System;
using System.Collections.Generic;

namespace DietWorker.Models;

public partial class MealHistory
{
    public int Id { get; set; }

    public string Date { get; set; } = null!;

    public string DishName { get; set; } = null!;

    public string? ProteinCategory { get; set; }

    public string TipologiaPiatto { get; set; } = null!;

    public string? CarbCategory { get; set; }

    public string? CookingType { get; set; }

    public int? VarietyScore { get; set; }
}
