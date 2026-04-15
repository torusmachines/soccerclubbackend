using System;
using System.Collections.Generic;

namespace FootballDashboardAPI.Models;

public partial class Sport
{
    public int SportId { get; set; }

    public string SportName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<SportActivity> SportActivities { get; set; } = new List<SportActivity>();
    public virtual ICollection<Player1> Player1s { get; set; } = new List<Player1>();
}