namespace UserService.Application.DTOs
{
    public class UserProfileQueryParametersDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }
}
