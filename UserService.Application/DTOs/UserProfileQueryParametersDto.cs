namespace UserService.Application.DTOs
{
    public class UserProfileQueryParametersDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string SearchTerm { get; set; } = string.Empty;
    }
}
