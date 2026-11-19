namespace Catalog.Contracts;

public record ProductDto(int Id, string Name, string Description, decimal Price);
public record CreateProductRequest(string Name, string? Description, decimal Price);
public record UpdateProductRequest(string Name, string? Description, decimal Price);
public record ApiError(string Code, string Message);