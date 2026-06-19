using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebQLministop.Data;

namespace WebQLministop.Controllers.Api;

[ApiController]
[Route("api/danh-muc")]
public class DanhMucApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DanhMucApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDanhSach()
    {
        var danhMucs = await _context.DanhMucs
            .AsNoTracking()
            .Where(d => d.KichHoat)
            .OrderBy(d => d.Ten)
            .Select(d => new
            {
                d.Id,
                d.Ten,
                d.MoTa
            })
            .ToListAsync();

        return Ok(danhMucs);
    }
}
