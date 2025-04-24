using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using QuickCashJobAPI.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using QuickCashJobAPI.Models.DTO;

namespace QuickCashJobAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IUserService _userService;

        public LocationController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDTO locationDto)
        {
            var userId = User.Identity?.Name; // Assume the user ID is the username
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _userService.UpdateUserLocationAsync(userId, locationDto.Latitude, locationDto.Longitude);
            return Ok();
        }
    }
}
