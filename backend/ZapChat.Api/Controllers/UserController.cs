using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZapChat.Api.Common.Response;
using ZapChat.Api.DTOs.Users;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
        => _userService = userService;

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // GET /api/user/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _userService.GetProfileAsync(GetUserId());
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    // GET /api/user/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetProfileAsync(id);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    // PUT /api/user/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest req)
    {
        var result = await _userService.UpdateProfileAsync(GetUserId(), req);
        return Ok(ApiResponse<UserResponse>.Ok(result, "Cập nhật profile thành công."));
    }

    // GET /api/user/search?keyword=abc
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var result = await _userService.SearchUsersAsync(keyword, GetUserId());
        return Ok(ApiResponse<List<UserResponse>>.Ok(result));
    }
}