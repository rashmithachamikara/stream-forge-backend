using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Engagement;
using StreamForge.Application.UseCases.Engagement;

namespace StreamForge.Api.Controllers.Videos;

/// <summary>
/// Exposes reactions, comments, and bookmarks endpoints for a single video.
/// </summary>
[ApiController]
[Route("api/v1/videos/{videoId:guid}")]
public sealed class VideoEngagementController : ControllerBase
{
    private readonly GetReactionSummaryService _getReactionSummary;
    private readonly SetReactionService _setReaction;
    private readonly RemoveReactionService _removeReaction;
    private readonly ListCommentsService _listComments;
    private readonly CreateCommentService _createComment;
    private readonly UpdateCommentService _updateComment;
    private readonly DeleteCommentService _deleteComment;
    private readonly ListVideoBookmarksService _listBookmarks;
    private readonly CreateBookmarkService _createBookmark;
    private readonly UpdateBookmarkService _updateBookmark;
    private readonly DeleteBookmarkService _deleteBookmark;

    public VideoEngagementController(
        GetReactionSummaryService getReactionSummary,
        SetReactionService setReaction,
        RemoveReactionService removeReaction,
        ListCommentsService listComments,
        CreateCommentService createComment,
        UpdateCommentService updateComment,
        DeleteCommentService deleteComment,
        ListVideoBookmarksService listBookmarks,
        CreateBookmarkService createBookmark,
        UpdateBookmarkService updateBookmark,
        DeleteBookmarkService deleteBookmark)
    {
        _getReactionSummary = getReactionSummary;
        _setReaction = setReaction;
        _removeReaction = removeReaction;
        _listComments = listComments;
        _createComment = createComment;
        _updateComment = updateComment;
        _deleteComment = deleteComment;
        _listBookmarks = listBookmarks;
        _createBookmark = createBookmark;
        _updateBookmark = updateBookmark;
        _deleteBookmark = deleteBookmark;
    }

    /// <summary>
    /// Gets the reaction summary for a video.
    /// </summary>
    [HttpGet("reactions/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ReactionSummaryDto>> GetReactionSummary(
        Guid videoId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _getReactionSummary.Handle(videoId, shareToken, cancellationToken));
    }

    /// <summary>
    /// Sets the authenticated user's reaction for a video.
    /// </summary>
    [HttpPut("reaction")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> SetReaction(
        Guid videoId,
        [FromBody] SetReactionRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _setReaction.Handle(videoId, request, cancellationToken));
    }

    /// <summary>
    /// Removes the authenticated user's reaction for a video.
    /// </summary>
    [HttpDelete("reaction")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> RemoveReaction(Guid videoId, CancellationToken cancellationToken)
    {
        return Ok(await _removeReaction.Handle(videoId, cancellationToken));
    }

    /// <summary>
    /// Lists comments for a video.
    /// </summary>
    [HttpGet("comments")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<CommentDto>>> ListComments(
        Guid videoId,
        [FromQuery] Guid? parentCommentId,
        [FromQuery] string? shareToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _listComments.Handle(new ListCommentsQuery(videoId, parentCommentId, page, pageSize), shareToken, cancellationToken));
    }

    /// <summary>
    /// Creates a new comment on a video.
    /// </summary>
    [HttpPost("comments")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> CreateComment(
        Guid videoId,
        [FromBody] CreateCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _createComment.Handle(videoId, request, cancellationToken));
    }

    /// <summary>
    /// Updates an existing comment on a video.
    /// </summary>
    [HttpPatch("comments/{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> UpdateComment(
        Guid videoId,
        Guid commentId,
        [FromBody] UpdateCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateComment.Handle(videoId, commentId, request, cancellationToken));
    }

    /// <summary>
    /// Deletes a comment from a video.
    /// </summary>
    [HttpDelete("comments/{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid videoId, Guid commentId, CancellationToken cancellationToken)
    {
        await _deleteComment.Handle(videoId, commentId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists bookmarks for a video owned by the authenticated user.
    /// </summary>
    [HttpGet("bookmarks")]
    [Authorize]
    public async Task<ActionResult<PagedResponseDto<BookmarkDto>>> ListBookmarks(
        Guid videoId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _listBookmarks.Handle(videoId, page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Creates a bookmark for a video.
    /// </summary>
    [HttpPost("bookmarks")]
    [Authorize]
    public async Task<ActionResult<BookmarkDto>> CreateBookmark(
        Guid videoId,
        [FromBody] CreateBookmarkRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _createBookmark.Handle(videoId, request, cancellationToken));
    }

    /// <summary>
    /// Updates a bookmark for a video.
    /// </summary>
    [HttpPatch("bookmarks/{bookmarkId:guid}")]
    [Authorize]
    public async Task<ActionResult<BookmarkDto>> UpdateBookmark(
        Guid videoId,
        Guid bookmarkId,
        [FromBody] UpdateBookmarkRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateBookmark.Handle(videoId, bookmarkId, request, cancellationToken));
    }

    /// <summary>
    /// Deletes a bookmark from a video.
    /// </summary>
    [HttpDelete("bookmarks/{bookmarkId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteBookmark(Guid videoId, Guid bookmarkId, CancellationToken cancellationToken)
    {
        await _deleteBookmark.Handle(videoId, bookmarkId, cancellationToken);
        return NoContent();
    }
}
