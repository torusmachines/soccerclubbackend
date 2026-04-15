using System;

namespace FootballDashboardAPI.Models;

public partial class SportActivity
{
    public int ActivityId { get; set; }

    public int SportId { get; set; }

    public string ActivityName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Sport? Sport { get; set; }
}