using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories;
using Npgsql;
using NpgsqlTypes;

namespace FootballDashboardAPI.Services;

public class ContactRoleService : IContactRoleService
{
    private readonly PostgresConnectionProvider _db;

    public ContactRoleService(PostgresConnectionProvider db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ContactRole>> GetAllAsync()
    {
        return await _db.ExecuteQueryListAsync(
            "SELECT * FROM stf.fn_contact_roles_get_all()",
            MapReaderToContactRole
        );
    }

    public async Task<ContactRole?> GetByIdAsync(string id)
    {
        return await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.fn_contact_roles_get_by_id(@p_id)",
            MapReaderToContactRole,
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = id }
        );
    }

    public async Task<ContactRole> CreateAsync(CreateContactRole c, string createdBy)
    {
        // Check if role name already exists
        var existingRole = await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.fn_contact_roles_get_by_name(@p_name)",
            MapReaderToContactRole,
            new NpgsqlParameter("p_name", NpgsqlDbType.Varchar) { Value = c.RoleName }
        );

        if (existingRole != null)
            throw new InvalidOperationException("Role name already exists");

        var roleId = Guid.NewGuid().ToString();
        var createdAt = DateTime.UtcNow;

        await _db.ExecuteNonQueryAsync(
            "SELECT stf.fn_contact_roles_insert(@p_role_id, @p_role_name, @p_description, @p_created_at, @p_created_by)",
            new NpgsqlParameter("p_role_id", NpgsqlDbType.Varchar) { Value = roleId },
            new NpgsqlParameter("p_role_name", NpgsqlDbType.Varchar) { Value = c.RoleName },
            new NpgsqlParameter("p_description", NpgsqlDbType.Text) { Value = (object?)c.Description ?? DBNull.Value },
            new NpgsqlParameter("p_created_at", NpgsqlDbType.Timestamp) { Value = DateTime.SpecifyKind(createdAt, DateTimeKind.Unspecified) },
            new NpgsqlParameter("p_created_by", NpgsqlDbType.Varchar) { Value = createdBy }
        );

        return await GetByIdAsync(roleId) ?? new ContactRole
        {
            RoleId = roleId,
            RoleName = c.RoleName,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };
    }

    public async Task<ContactRole?> UpdateAsync(string id, UpdateContactRole c)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null)
            return null;

        // Check if role name already exists for another role
        var existingRole = await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.fn_contact_roles_get_by_name(@p_name)",
            MapReaderToContactRole,
            new NpgsqlParameter("p_name", NpgsqlDbType.Varchar) { Value = c.RoleName }
        );

        if (existingRole != null && existingRole.RoleId != id)
            throw new InvalidOperationException("Role name already exists");

        await _db.ExecuteNonQueryAsync(
            "SELECT stf.fn_contact_roles_update(@p_role_id, @p_role_name, @p_description)",
            new NpgsqlParameter("p_role_id", NpgsqlDbType.Varchar) { Value = id },
            new NpgsqlParameter("p_role_name", NpgsqlDbType.Varchar) { Value = c.RoleName },
            new NpgsqlParameter("p_description", NpgsqlDbType.Text) { Value = (object?)c.Description ?? DBNull.Value }
        );

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        // Check if role is being used by any contacts
        var usageCount = await _db.ExecuteScalarAsync(
            "SELECT COUNT(*) FROM stf.club_contacts WHERE role_name = (SELECT role_name FROM stf.contact_roles WHERE role_id = @p_id)",
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = id }
        );

        if (Convert.ToInt32(usageCount ?? 0) > 0)
            throw new InvalidOperationException("Cannot delete role that is currently assigned to contacts");

        // Call the delete function
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand("DELETE FROM stf.contact_roles WHERE role_id = @p_id", connection))
            {
                command.Parameters.AddWithValue("@p_id", id);
                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }
    }

    private static ContactRole MapReaderToContactRole(NpgsqlDataReader reader)
    {
        return new ContactRole
        {
            RoleId = reader.GetString(reader.GetOrdinal("role_id")),
            RoleName = reader.GetString(reader.GetOrdinal("role_name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            CreatedBy = reader.GetString(reader.GetOrdinal("created_by"))
        };
    }
}