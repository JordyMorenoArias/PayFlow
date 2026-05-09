namespace AuthService.Domain.Entities
{
    public class AuthUser
    {
        public Guid Id { get; private set; }

        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

        private AuthUser() { }

        public AuthUser(string email, string passwordHash)
        {
            Id = Guid.NewGuid();
            Email = NormalizeEmail(email);
            PasswordHash = passwordHash;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void UpdatePassword(string newPasswordHash)
        {
            PasswordHash = newPasswordHash;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
