using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class ReviewSkillDetailRepository : IReviewSkillDetailRepository
{
    private readonly FootballContext _footballContext;

    public ReviewSkillDetailRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<ReviewSkillDetail>> GetAllAsync()
    {
        return await _footballContext.ReviewSkillDetails
            .AsNoTracking()
            .OrderBy(r => r.ReviewId)
            .ThenBy(r => r.SkillKey)
            .ToListAsync();
    }

    public async Task<ReviewSkillDetail?> GetByIdAsync(string reviewId, string skillKey)
    {
        return await _footballContext.ReviewSkillDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.SkillKey == skillKey);
    }

    public async Task<IEnumerable<ReviewSkillDetail>> GetByReviewIdAsync(string reviewId)
    {
        return await _footballContext.ReviewSkillDetails
            .AsNoTracking()
            .Where(r => r.ReviewId == reviewId)
            .OrderBy(r => r.SkillKey)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string reviewId, string skillKey)
    {
        return await _footballContext.ReviewSkillDetails
            .AsNoTracking()
            .AnyAsync(r => r.ReviewId == reviewId && r.SkillKey == skillKey);
    }

    public async Task<ReviewSkillDetail> CreateAsync(ReviewSkillDetail detail)
    {
        _footballContext.ReviewSkillDetails.Add(detail);
        await _footballContext.SaveChangesAsync();

        return detail;
    }

    public async Task<ReviewSkillDetail?> UpdateAsync(ReviewSkillDetail detail)
    {
        var existing = await _footballContext.ReviewSkillDetails
            .FirstOrDefaultAsync(r => r.ReviewId == detail.ReviewId && r.SkillKey == detail.SkillKey);

        if (existing == null) return null;

        existing.Rating = detail.Rating;
        existing.CommentText = detail.CommentText;
        existing.FollowUpDate = detail.FollowUpDate;

        await _footballContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string reviewId, string skillKey)
    {
        var existing = await _footballContext.ReviewSkillDetails
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.SkillKey == skillKey);

        if (existing == null)
            return false;

        _footballContext.ReviewSkillDetails.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }
}
