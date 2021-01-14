using System;
using System.Diagnostics.CodeAnalysis;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class UserGroup
    {
        public Guid Users_Id { get; set; }

        public Guid Groups_Id { get; set; }
    }
}