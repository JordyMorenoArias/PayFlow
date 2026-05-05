namespace AuthService.Application.DTOs
{
    public class LoginAuthUserDto
    {
        public string Email { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
    }
}
