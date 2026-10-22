using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhdphuong_1787_api.Models;

namespace nhdphuong_1787_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlertLogApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AlertLogApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AlertLogApi?childId=1
        [HttpGet]
        public async Task<IActionResult> GetAlerts([FromQuery] int? childId)
        {
            var query = _context.AlertLogs.AsQueryable();
            if (childId.HasValue && childId.Value > 0)
            {
                query = query.Where(a => a.ChildProfileId == childId.Value);
            }

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(alerts);
        }

        // POST: api/AlertLogApi
        [HttpPost]
        [AllowAnonymous] // Cho phép máy trẻ gửi cảnh báo lên mà không cần phức tạp hóa token
        public async Task<IActionResult> CreateAlert([FromBody] AlertLog model)
        {
            model.CreatedAt = DateTime.UtcNow;
            _context.AlertLogs.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }
    }
}
