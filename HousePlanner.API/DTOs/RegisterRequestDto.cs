namespace HousePlanner.API.DTOs
{
    public class RegisterRequestDto
    {
        /// <summary>
        /// The full name the applicant wishes to register under.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// The publicly allowed role the user is requesting. Must be one of:
        /// "Customer" or "Architect".
        /// Admin and privileged roles are explicitly rejected.
        /// </summary>
        public string RequestedRole { get; set; } = string.Empty;
    }
}
