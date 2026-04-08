using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories;
using Npgsql;
using NpgsqlTypes;

namespace FootballDashboardAPI.Services;

public class PlayerPositionService : IPlayerPositionService
{
    private readonly PostgresConnectionProvider _db;

    public PlayerPositionService(PostgresConnectionProvider db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PlayerPosition>> GetAllAsync()
    {
        return await _db.ExecuteQueryListAsync(
            "SELECT * FROM stf.fn_player_positions_get_all()",
            MapReaderToPlayerPosition
        );
    }

    public async Task<PlayerPosition?> GetByIdAsync(string id)
    {
        return await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.fn_player_positions_get_by_id(@p_id)",
            MapReaderToPlayerPosition,
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = id }
        );
    }

    public async Task<PlayerPosition> CreateAsync(CreatePlayerPosition dto, string createdBy)
    {
        // Check if position code already exists
        var existingPosition = await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.fn_player_positions_get_by_code(@p_code)",
            MapReaderToPlayerPosition,
            new NpgsqlParameter("p_code", NpgsqlDbType.Varchar) { Value = dto.PositionCode }
        );

        if (existingPosition != null)
            throw new InvalidOperationException("Position code already exists");

        var positionId = Guid.NewGuid().ToString();
        var createdAt = DateTime.UtcNow;

        await _db.ExecuteNonQueryAsync(
            "SELECT stf.fn_player_positions_insert(@p_position_id, @p_position_code, @p_position_name, @p_description, @p_created_at, @p_created_by)",
            new NpgsqlParameter("p_position_id", NpgsqlDbType.Varchar) { Value = positionId },
            new NpgsqlParameter("p_position_code", NpgsqlDbType.Varchar) { Value = dto.PositionCode },
            new NpgsqlParameter("p_position_name", NpgsqlDbType.Varchar) { Value = dto.PositionName },
            new NpgsqlParameter("p_description", NpgsqlDbType.Text) { Value = (object?)dto.Description ?? DBNull.Value },
            new NpgsqlParameter("p_created_at", NpgsqlDbType.Timestamp) { Value = DateTime.SpecifyKind(createdAt, DateTimeKind.Unspecified) },
            new NpgsqlParameter("p_created_by", NpgsqlDbType.Varchar) { Value = createdBy }
        );

        return await GetByIdAsync(positionId) ?? new PlayerPosition
        {
            PositionId = positionId,
            PositionCode = dto.PositionCode,
            PositionName = dto.PositionName,
            Description = dto.Description,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };
    }

    public async Task<PlayerPosition?> UpdateAsync(string id, UpdatePlayerPosition dto)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null)
            return null;

        // Check if position code already exists for another position
        var existingPosition = await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.fn_player_positions_get_by_code(@p_code)",
            MapReaderToPlayerPosition,
            new NpgsqlParameter("p_code", NpgsqlDbType.Varchar) { Value = dto.PositionCode }
        );

        if (existingPosition != null && existingPosition.PositionId != id)
            throw new InvalidOperationException("Position code already exists");

        await _db.ExecuteNonQueryAsync(
            "SELECT stf.fn_player_positions_update(@p_position_id, @p_position_code, @p_position_name, @p_description)",
            new NpgsqlParameter("p_position_id", NpgsqlDbType.Varchar) { Value = id },
            new NpgsqlParameter("p_position_code", NpgsqlDbType.Varchar) { Value = dto.PositionCode },
            new NpgsqlParameter("p_position_name", NpgsqlDbType.Varchar) { Value = dto.PositionName },
            new NpgsqlParameter("p_description", NpgsqlDbType.Text) { Value = (object?)dto.Description ?? DBNull.Value }
        );

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        // Check if position is being used by any players
        var usageCount = await _db.ExecuteScalarAsync(
            "SELECT COUNT(*) FROM stf.players WHERE position_code = (SELECT position_code FROM stf.player_positions WHERE position_id = @p_id)",
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = id }
        );

        if (Convert.ToInt32(usageCount ?? 0) > 0)
            throw new InvalidOperationException("Cannot delete position that is currently assigned to players");

        // Call the delete function
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand("DELETE FROM stf.player_positions WHERE position_id = @p_id", connection))
            {
                command.Parameters.AddWithValue("@p_id", id);
                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }
    }

    private static PlayerPosition MapReaderToPlayerPosition(NpgsqlDataReader reader)
    {
        return new PlayerPosition
        {
            PositionId = reader.GetString(reader.GetOrdinal("position_id")),
            PositionCode = reader.GetString(reader.GetOrdinal("position_code")),
            PositionName = reader.GetString(reader.GetOrdinal("position_name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            CreatedBy = reader.GetString(reader.GetOrdinal("created_by"))
        };
    }
}
