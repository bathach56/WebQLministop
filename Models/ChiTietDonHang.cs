using System.ComponentModel.DataAnnotations;

namespace WebQLministop.Models;

public class ChiTietDonHang
{
    public int Id { get; set; }

    public int DonHangId { get; set; }
    public DonHang? DonHang { get; set; }

    public int SanPhamId { get; set; }
    public SanPham? SanPham { get; set; }

    [StringLength(50)]
    public string? MaSanPham { get; set; }

    [StringLength(150)]
    public string? TenSanPham { get; set; }

    [StringLength(30)]
    public string? DonViSanPham { get; set; }

    public int SoLuong { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DonGia { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TienGiam { get; set; }
}
