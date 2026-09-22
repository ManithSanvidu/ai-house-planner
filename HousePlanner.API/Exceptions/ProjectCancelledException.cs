using System;

namespace HousePlanner.API.Exceptions
{
    public class ProjectCancelledException : InvalidOperationException
    {
        public ProjectCancelledException() : base("This construction project has been cancelled and is read-only.")
        {
        }
    }
}
