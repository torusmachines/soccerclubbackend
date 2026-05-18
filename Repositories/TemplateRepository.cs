using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly FootballContext _footballContext;

    public TemplateRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<Template>> GetAllAsync()
    {
        return await _footballContext.Templates
            .AsNoTracking()
            .OrderBy(t => t.TemplateName)
            .ToListAsync();
    }

    public async Task<Template?> GetByIdAsync(string id)
    {
        return await _footballContext.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateId == id);
    }

    public async Task<Template> CreateAsync(Template template)
    {
        var templateIds = await _footballContext.Templates
            .AsNoTracking()
            .Select(t => t.TemplateId)
            .ToListAsync();

        var lastId = templateIds
            .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
            .Max();

        template.TemplateId = ((lastId ?? 0) + 1).ToString();

        if (template.CreatedAt == default)
            template.CreatedAt = DateTime.UtcNow;

        _footballContext.Templates.Add(template);
        await _footballContext.SaveChangesAsync();

        return template;
    }

    public async Task<Template?> UpdateAsync(Template template)
    {
        var existing = await _footballContext.Templates
            .FirstOrDefaultAsync(t => t.TemplateId == template.TemplateId);

        if (existing == null)
            return null;

        existing.TemplateName = template.TemplateName;
        existing.TemplateType = template.TemplateType;
        existing.Subject = template.Subject;
        existing.Body = template.Body;

        await _footballContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _footballContext.Templates
            .FirstOrDefaultAsync(t => t.TemplateId == id);

        if (existing == null)
            return false;

        _footballContext.Templates.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _footballContext.Templates
            .AsNoTracking()
            .AnyAsync(t => t.TemplateId == id);
    }

    public async Task<bool> TemplateNameExistsAsync(string templateName, string? excludeTemplateId = null)
    {
        var normalizedName = templateName.Trim().ToLower();

        return await _footballContext.Templates
            .AsNoTracking()
            .AnyAsync(t =>
                t.TemplateId != excludeTemplateId &&
                t.TemplateName.ToLower() == normalizedName);
    }
}
