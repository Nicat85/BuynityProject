namespace OnlineShppingSystem.Application.DTOs.CategoriesDtos;

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
}