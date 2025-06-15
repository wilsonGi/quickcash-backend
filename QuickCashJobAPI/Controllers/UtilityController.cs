using Microsoft.AspNetCore.Mvc;

namespace QuickCashJobAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UtilityController : ControllerBase
    {
        [HttpGet("get-ip")]
        public async Task<IActionResult> GetIp()
        {
            using var httpClient = new HttpClient();
            var ip = await httpClient.GetStringAsync("https://ifconfig.me/ip");
            return Ok(ip);
        }
    }
}
