using System.ComponentModel.DataAnnotations;

namespace WebQLministop.Models;

public class DanhMuc
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Ten { get; set; } = string.Empty;

    [StringLength(250)]
    public string? MoTa { get; set; }

    public bool KichHoat { get; set; } = true;
}
