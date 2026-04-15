using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballDashboardAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ReviewActivityRatingsController : ControllerBase
{
    private readonly IReviewActivityRatingRepository _repository;

    public ReviewActivityRatingsController(IReviewActivityRatingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewActivityRating>>> GetAll()
    {
        var ratings = await _repository.GetAllAsync();
        return Ok(ratings);
    }

    [HttpGet("review/{reviewId}")]
    public async Task<ActionResult<IEnumerable<ReviewActivityRating>>> GetByReviewId(string reviewId)
    {
        var ratings = await _repository.GetByReviewIdAsync(reviewId);
        return Ok(ratings);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<ReviewActivityRating>>> BulkCreate([FromBody] CreateReviewActivityRatingsRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ReviewId) || request.Ratings == null)
        {
            return BadRequest(new { message = "Invalid review activity rating payload." });
        }

        var createdRatings = new List<ReviewActivityRating>();

        foreach (var payload in request.Ratings)
        {
            var rating = new ReviewActivityRating
            {
                ReviewId = request.ReviewId,
                ActivityId = payload.ActivityId,
                Rating = payload.Rating,
                Comment = payload.Comment,
                RatingFollowupDate = payload.RatingFollowupDate
            };

            var created = await _repository.CreateAsync(rating);
            createdRatings.Add(created);
        }

        return Ok(createdRatings);
    }
}
