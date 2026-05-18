using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class UserRepository : IUserRepository
{
    private readonly FootballContext _footballContext;

    public UserRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _footballContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _footballContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> CreateAsync(User user)
    {
        user.CreatedAt ??= DateTime.UtcNow;

        _footballContext.Users.Add(user);
        await _footballContext.SaveChangesAsync();

        return user;
    }

    public async Task<User?> UpdateAsync(User user)
    {
        var existingUser = await _footballContext.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (existingUser == null)
            return null;

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.Password = user.Password;
        existingUser.Role = user.Role;
        existingUser.Phone = user.Phone;
        existingUser.Status = user.Status;
        existingUser.UpdatedAt = user.UpdatedAt ?? DateTime.UtcNow;

        await _footballContext.SaveChangesAsync();

        return existingUser;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var existing = await _footballContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (existing == null)
            return false;

        _footballContext.Users.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _footballContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email, long? excludeUserId = null)
    {
        var normalizedEmail = email.Trim().ToLower();

        return await _footballContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != excludeUserId &&
                u.Email.ToLower() == normalizedEmail);
    }
}
