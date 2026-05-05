namespace AuthService.Application.DTOs
{
    public class RegisterAuthUserDto
    {
        public string Email { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
    }
}
