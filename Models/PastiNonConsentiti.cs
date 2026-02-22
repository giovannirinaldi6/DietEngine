using System;
using System.Collections.Generic;

namespace DietWorker.Models;

public partial class PastiNonConsentiti
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Reason { get; set; }

    public string AddedOn { get; set; } = null!;
}
