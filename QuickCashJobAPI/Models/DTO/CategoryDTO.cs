namespace QuickCashJobAPI.Models.DTO
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public int NumberOfInstances { get; set; }
        public string? CategoryImage { get; set; }
    }
}
