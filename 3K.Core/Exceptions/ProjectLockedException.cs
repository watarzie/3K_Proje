using System;

namespace _3K.Core.Exceptions
{
    public class ProjectLockedException : Exception
    {
        public ProjectLockedException(string message) : base(message)
        {
        }
    }
}
