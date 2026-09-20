namespace HousePlanner.API.DTOs
{
    public class UserInfoResponseDto
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // e.g. "Customer", "Architect"
    }
}

