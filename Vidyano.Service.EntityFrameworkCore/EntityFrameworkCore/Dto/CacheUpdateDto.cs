using System;
using System.ComponentModel.DataAnnotations;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    /// <summary>
    /// Represents the object for a cache update in the repository (to pull/push changes across multiple running instances).
    /// </summary>
    public class CacheUpdateDto
    {
        /// <summary>
        /// Gets or sets the unique key for this cache update instance.
        /// </summary>
        [Required]
        public virtual Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the moment the change happened.
        /// </summary>
        [Required]
        public virtual DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the data for the change (Can be an empty array if everything or too much has changed).
        /// </summary>
        [Required]
        public virtual byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets the optional type of content in the <see cref="Value"/> property.
        /// </summary>
        public virtual CacheUpdateChange? Change { get; set; }

        /// <summary>
        /// Gets or sets the optional value containing the changed content.
        /// </summary>
        public virtual string? Value { get; set; }
    }

    /// <summary>
    /// Describes the contents of the <see cref="CacheUpdateDto.Value"/> property.
    /// </summary>
    public enum CacheUpdateChange : byte
    {
        /// <summary>
        /// Multiple changes, <see cref="CacheUpdateDto.Value"/> will contain a semicolon separated list of property names.
        /// </summary>
        Multiple = 0,

        /// <summary>
        /// A single user's LastLoginDate property was updated, <see cref="CacheUpdateDto.Value"/> will contain the service string value.
        /// </summary>
        UserLastLoginDate = 1,

        /// <summary>
        /// A single user's Settings property was updated, <see cref="CacheUpdateDto.Value"/> will contain the value.
        /// </summary>
        UserSettings = 2,

        /// <summary>
        /// A single user's Profile property was updated, <see cref="CacheUpdateDto.Value"/> will contain the value.
        /// </summary>
        UserProfile = 3,

        /// <summary>
        /// A Setting's Value was updated, <see cref="CacheUpdateDto.Value"/> will contain the value.
        /// </summary>
        SettingValue = 4,
    }
}