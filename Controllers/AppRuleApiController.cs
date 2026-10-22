using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhdphuong_1787_api.Models;

namespace nhdphuong_1787_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppRuleApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppRuleApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AppRuleApi?childId=1
        [HttpGet]
        public async Task<IActionResult> GetRules([FromQuery] int childId)
        {
            var rules = await _context.AppRules
                .Where(r => r.ChildProfileId == childId)
                .OrderBy(r => r.Category)
                .ThenBy(r => r.AppName)
                .ToListAsync();

            return Ok(rules);
        }

        // POST: api/AppRuleApi
        [HttpPost]
        public async Task<IActionResult> CreateRule([FromBody] AppRule model)
        {
            var childExists = await _context.ChildProfiles.AnyAsync(c => c.Id == model.ChildProfileId);
            if (!childExists)
                return NotFound(new { message = "Không tìm thấy hồ sơ bé tương ứng!" });

            model.CreatedAt = DateTime.UtcNow;
            _context.AppRules.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        // PUT: api/AppRuleApi/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRule(int id, [FromBody] AppRule model)
        {
            var existing = await _context.AppRules.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy quy tắc!" });

            existing.AppName = model.AppName;
            existing.Category = model.Category;
            existing.Status = model.Status;
            existing.DailyLimitMinutes = model.DailyLimitMinutes;
            existing.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // PATCH: api/AppRuleApi/{id}/toggle
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleRule(int id)
        {
            var existing = await _context.AppRules.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy quy tắc!" });

            existing.IsActive = !existing.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = existing.IsActive ? "Đã bật quy tắc bảo vệ!" : "Đã tạm dừng quy tắc!",
                isActive = existing.IsActive
            });
        }

        // DELETE: api/AppRuleApi/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            var existing = await _context.AppRules.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy quy tắc!" });

            _context.AppRules.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa quy tắc thành công!" });
        }
    }
}
