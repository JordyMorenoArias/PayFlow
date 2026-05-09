using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs
{
    public class AuthUserResultDto
    {
        public string Token { get; set; } = string.Empty;

        public string? RefreshToken { get; set; }

        public DateTimeOffset Expires { get; set; }
    }
}
