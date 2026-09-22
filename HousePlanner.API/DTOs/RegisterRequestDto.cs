namespace HousePlanner.API.DTOs
{
    public class RegisterRequestDto
    {
        /// <summary>
        /// The full name the applicant wishes to register under.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        // Public registration is always Customer. Role, RoleId, and SupabaseUid are
        // deliberately absent because those values are server-owned decisions.
    }
}
