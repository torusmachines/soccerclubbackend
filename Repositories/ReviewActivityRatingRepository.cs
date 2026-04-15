using FootballDashboardAPI.Models;
using Npgsql;
using NpgsqlTypes;

namespace FootballDashboardAPI.Repositories;

public class ReviewActivityRatingRepository : IReviewActivityRatingRepository
{
    private readonly PostgresConnectionProvider _db;

    public ReviewActivityRatingRepository(PostgresConnectionProvider db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ReviewActivityRating>> GetAllAsync()
    {
        return await _db.ExecuteQueryListAsync(
            "SELECT * FROM stf.review_activity_ratings ORDER BY review_activity_rating_id",
            MapReaderToReviewActivityRating
        );
    }

    public async Task<IEnumerable<ReviewActivityRating>> GetByReviewIdAsync(string reviewId)
    {
        return await _db.ExecuteQueryListAsync(
            "SELECT * FROM stf.fn_review_activity_ratings_get_by_review_id(@p_review_id)",
            MapReaderToReviewActivityRating,
            new NpgsqlParameter("p_review_id", NpgsqlDbType.Varchar) { Value = reviewId }
        );
    }

    public async Task<ReviewActivityRating> CreateAsync(ReviewActivityRating rating)
    {
        await _db.ExecuteNonQueryAsync(
            "SELECT stf.fn_review_activity_ratings_insert(@p_review_id, @p_activity_id, @p_rating, @p_comment, @p_rating_followup_date)",
            new NpgsqlParameter("p_review_id", NpgsqlDbType.Varchar) { Value = rating.ReviewId },
            new NpgsqlParameter("p_activity_id", NpgsqlDbType.Integer) { Value = rating.ActivityId },
            new NpgsqlParameter("p_rating", NpgsqlDbType.Numeric) { Value = rating.Rating },
            new NpgsqlParameter("p_comment", NpgsqlDbType.Text) { Value = rating.Comment == null ? DBNull.Value : (object)rating.Comment },
            new NpgsqlParameter("p_rating_followup_date", NpgsqlDbType.Timestamp) { Value = rating.RatingFollowupDate == null ? DBNull.Value : (object)DateTime.SpecifyKind(rating.RatingFollowupDate.Value, DateTimeKind.Unspecified) }
        );

        return await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.review_activity_ratings WHERE review_id = @p_review_id AND activity_id = @p_activity_id ORDER BY review_activity_rating_id DESC LIMIT 1",
            MapReaderToReviewActivityRating,
            new NpgsqlParameter("p_review_id", NpgsqlDbType.Varchar) { Value = rating.ReviewId },
            new NpgsqlParameter("p_activity_id", NpgsqlDbType.Integer) { Value = rating.ActivityId }
        ) ?? rating;
    }

    private ReviewActivityRating MapReaderToReviewActivityRating(NpgsqlDataReader reader)
    {
        return new ReviewActivityRating
        {
            ReviewActivityRatingId = Convert.ToInt32(reader["review_activity_rating_id"]),
            ReviewId = reader["review_id"].ToString()!,
            ActivityId = Convert.ToInt32(reader["activity_id"]),
            Rating = Convert.ToDecimal(reader["rating"]),
            Comment = reader["comment"] == DBNull.Value ? null : reader["comment"].ToString(),
            RatingFollowupDate = reader["rating_followup_date"] == DBNull.Value ? null : (DateTime?)reader["rating_followup_date"],
            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.MinValue : (DateTime)reader["created_at"],
            UpdatedAt = reader["updated_at"] == DBNull.Value ? DateTime.MinValue : (DateTime)reader["updated_at"],
        };
    }
}
