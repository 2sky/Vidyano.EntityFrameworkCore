using System;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    internal class UserSettings
    {
        public Guid Id { get; set; }

        public string Settings { get; set; }
    }
}