using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhdphuong_1787_api.Models;

namespace nhdphuong_1787_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScreenTimeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScreenTimeApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ScreenTimeApi?childId=1
        [HttpGet]
        public async Task<IActionResult> GetSchedules([FromQuery] int childId)
        {
            var schedules = await _context.ScreenTimeSchedules
                .Where(s => s.ChildProfileId == childId)
                .OrderBy(s => s.ScheduleTitle)
                .ToListAsync();

            return Ok(schedules);
        }

        // POST: api/ScreenTimeApi
        [HttpPost]
        public async Task<IActionResult> CreateSchedule([FromBody] ScreenTimeSchedule model)
        {
            var childExists = await _context.ChildProfiles.AnyAsync(c => c.Id == model.ChildProfileId);
            if (!childExists)
                return NotFound(new { message = "Không tìm thấy hồ sơ bé tương ứng!" });

            model.CreatedAt = DateTime.UtcNow;
            _context.ScreenTimeSchedules.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        // PUT: api/ScreenTimeApi/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScreenTimeSchedule model)
        {
            var existing = await _context.ScreenTimeSchedules.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy lịch biểu!" });

            existing.ScheduleTitle = model.ScheduleTitle;
            existing.DayOfWeek = model.DayOfWeek;
            existing.AllowedStartTime = model.AllowedStartTime;
            existing.AllowedEndTime = model.AllowedEndTime;
            existing.MaxDailyHours = model.MaxDailyHours;
            existing.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE: api/ScreenTimeApi/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var existing = await _context.ScreenTimeSchedules.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy lịch biểu!" });

            _context.ScreenTimeSchedules.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa lịch biểu thành công!" });
        }
    }
}
