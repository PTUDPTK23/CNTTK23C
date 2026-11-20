using System.ComponentModel.DataAnnotations;

namespace Auction.Application.Dtos;

public class CreateProductRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm."), MinLength(2, ErrorMessage = "Tên sản phẩm phải có ít nhất 2 ký tự."), MaxLength(160, ErrorMessage = "Tên sản phẩm tối đa 160 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả."), MinLength(10, ErrorMessage = "Mô tả phải có ít nhất 10 ký tự."), MaxLength(4000, ErrorMessage = "Mô tả tối đa 4000 ký tự.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập bộ sưu tập / danh mục."), MinLength(2, ErrorMessage = "Bộ sưu tập phải có ít nhất 2 ký tự."), MaxLength(60, ErrorMessage = "Bộ sưu tập tối đa 60 ký tự.")]
    public string Category { get; set; } = string.Empty;

    [MaxLength(1024, ErrorMessage = "Đường dẫn hình ảnh tối đa 1024 ký tự.")]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(0.01, 999_999_999, ErrorMessage = "Giá khởi điểm phải lớn hơn 0.")]
    public decimal StartingPrice { get; set; }
}

public sealed class UpdateProductRequest : CreateProductRequest;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string ImageUrl,
    decimal StartingPrice,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int AuctionCount);
