using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Models;

[Table("contact_roles", Schema = "stf")]
public partial class ContactRole
{
    [Key]
    [Column("role_id")]
    [StringLength(50)]
    public string RoleId { get; set; } = null!;

    [Column("role_name")]
    [StringLength(100)]
    public string RoleName { get; set; } = null!;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    [StringLength(50)]
    public string CreatedBy { get; set; } = null!;
}

public class CreateContactRole
{
    [Required]
    [StringLength(100)]
    public string RoleName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class UpdateContactRole
{
    [Required]
    [StringLength(100)]
    public string RoleName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }
}