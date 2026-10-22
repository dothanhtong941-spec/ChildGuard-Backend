using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhdphuong_1787_api.Models;

namespace nhdphuong_1787_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StaffCatalogApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StaffCatalogApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/StaffCatalogApi
        [HttpGet]
        public async Task<IActionResult> GetCatalog()
        {
            var catalog = await _context.StaffCatalogItems
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(catalog);
        }

        // POST: api/StaffCatalogApi
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AddCatalogItem([FromBody] StaffCatalogItem item)
        {
            item.CreatedByStaff = User.FindFirst(ClaimTypes.Name)?.Value ?? "staff";
            item.CreatedAt = DateTime.UtcNow;

            _context.StaffCatalogItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        // DELETE: api/StaffCatalogApi/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteCatalogItem(int id)
        {
            var item = await _context.StaffCatalogItems.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy mục trong thư viện mẫu!" });

            _context.StaffCatalogItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa mục khỏi thư viện mẫu!" });
        }
    }
}
