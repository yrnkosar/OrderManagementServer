using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly IUserService _userService;

        public LogController(ILogService logService, IUserService userService)
        {
            _logService = logService;
            _userService = userService;
        }
        // GET api/log
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Log>>> GetLogs()
        {
            var logs = await _logService.GetLogsAsync();
            if (logs == null)
            {
                return NotFound();
            }
            return Ok(logs);  // Swagger üzerinden erişilebilir
        }

        [HttpGet("my-logs")]
        public async Task<ActionResult<IEnumerable<Log>>> GetMyLogs()
        {
            // Oturum açmış kullanıcının kimliğini al
            var user = User;
            var userId = await _userService.GetCurrentUserIdAsync(user);

            if (userId == null)
                return Unauthorized("Geçersiz kullanıcı bilgisi");

            // Kullanıcının loglarını al
            var myLogs = await _logService.GetLogsByCustomerIdAsync(int.Parse(userId));

            if (myLogs == null || !myLogs.Any())
                return NotFound("Hiç log bulunamadı.");

            return Ok(myLogs);
        }

    }
}
