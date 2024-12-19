using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Services;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [Authorize]
    public class LogController : Controller
    {
        private readonly ILogService _logService;

        // LogService bağımlılığı ekleniyor
        public LogController(ILogService logService)
        {
            _logService = logService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _logService.GetLogsAsync();
            return Ok(logs);
        }
    }
}
