namespace OnlineShppingSystem.Application.DTOs.CategoriesDtos;

public class UpdateCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
}
