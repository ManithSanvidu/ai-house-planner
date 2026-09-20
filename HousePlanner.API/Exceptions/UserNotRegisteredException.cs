using System;

namespace HousePlanner.API.Exceptions;

/// <summary>
/// Thrown when a valid Firebase authentication token is provided, 
/// but the corresponding application user profile does not exist in the database.
/// Used to trigger the Google onboarding flow.
/// </summary>
public class UserNotRegisteredException : Exception
{
    public UserNotRegisteredException() 
        : base("User is authenticated but not registered in the application.")
    {
    }

    public UserNotRegisteredException(string message) 
        : base(message)
    {
    }

    public UserNotRegisteredException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
