using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;

        // LogService bağımlılığı ekleniyor
        public LogController(ILogService logService)
        {
            _logService = logService;
        }

        // GET api/log
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Log>>> GetLogs()
        {
            var logs = await _logService.GetLogsAsync();
            if (logs == null)
            {
                return NotFound();
            }
            return Ok(logs);  // Swagger üzerinden erişilebilir
        }
    }
}
