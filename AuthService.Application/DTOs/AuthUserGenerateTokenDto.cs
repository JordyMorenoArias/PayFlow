namespace AuthService.Application.DTOs
{
    public class AuthUserGenerateTokenDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
