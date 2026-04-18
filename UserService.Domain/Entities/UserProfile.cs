namespace UserService.Domain.Entities
{
    public class UserProfile
    {
        public Guid Id { get; private set; }

        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? DeletedAt { get; set; }

        private UserProfile() { } // EF Core

        public UserProfile(Guid id, string firstName, string lastName)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
            DeletedAt = null;
        }

        public void Update(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Delete()
        {
            DeletedAt = DateTimeOffset.UtcNow;
        }
    }
}
