using System;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    internal class UserProfile
    {
        public Guid Id { get; set; }

        public string Profile { get; set; }
    }
}