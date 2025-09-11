namespace SkillForge.Api.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        
        // Backward compatibility with old name
        public int Limit 
        { 
            get => PageSize; 
            set => PageSize = value; 
        }
    }
}