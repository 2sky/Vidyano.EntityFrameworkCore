using System;
using Microsoft.EntityFrameworkCore;
using Vidyano.Service.EntityFrameworkCore.Dto;
using Vidyano.Service.Repository.DataLayer;

namespace Vidyano.Service.EntityFrameworkCore
{
    internal static class SqlStatements
    {
        private const string schemaName = "Vidyano";

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Non-tracked entities
            modelBuilder.Entity<CacheUpdateDto>().ToTable(nameof(DefaultRepositoryProvider.CacheUpdates), schemaName)
                .Property(cu => cu.Timestamp).ValueGeneratedOnAddOrUpdate();
            modelBuilder.Entity<RegisteredStreamDto>().ToTable(nameof(DefaultRepositoryProvider.RegisteredStreams), schemaName)
                .Ignore(rs => rs.UserId);
            var feedback = modelBuilder.Entity<FeedbackDto>().ToTable(nameof(DefaultRepositoryProvider.Feedbacks), schemaName);
            feedback.Property(f => f.CreatedOn).HasColumnType("datetimeoffset(3)");
            var log = modelBuilder.Entity<LogDto>().ToTable(nameof(DefaultRepositoryProvider.Logs), schemaName);
            log.Property<long>("DbId").HasColumnName("Id").UseIdentityColumn();
            log.Property(l => l.Id).HasColumnName("ExternalId");
            log.Property(l => l.CreatedOn).HasColumnType("datetimeoffset(3)");

            // Tracked entities
            modelBuilder.Entity<SettingDto>().ToTable(nameof(DefaultRepositoryProvider.Settings), schemaName);
            modelBuilder.Entity<UserNotificationDto>().ToTable(nameof(DefaultRepositoryProvider.UserNotifications), schemaName);
            modelBuilder.Entity<UserProfile>().ToTable(nameof(DefaultRepositoryProvider.UserProfiles), schemaName);
            modelBuilder.Entity<UserSettings>().ToTable(nameof(DefaultRepositoryProvider.UserSettings), schemaName);

            var user = modelBuilder.Entity<UserDto>().ToTable(nameof(DefaultRepositoryProvider.Users), schemaName);
            user.Property(u => u.Language).IsUnicode(false);
            user.Property(u => u.CultureInfo).IsUnicode(false);
            user.Property(u => u.Version).IsUnicode(false);
            user.Property(u => u.TwoFactorToken).IsUnicode(false);
            user.Property(u => u.CreationDate).HasColumnType("datetimeoffset(3)");
            user.Property(u => u.LastLoginDate).HasColumnType("datetimeoffset(3)");
            var group = modelBuilder.Entity<GroupDto>().ToTable(nameof(DefaultRepositoryProvider.Groups), schemaName);
            group.Property(g => g.CreationDate).HasColumnType("datetimeoffset(3)");
            //modelBuilder.Entity<UserNotification>().ToTable("UserNotifications", schemaName);
            modelBuilder.Entity<UserGroup>().ToTable("UserGroup", schemaName)
                .HasKey(ug => new { ug.Users_Id, ug.Groups_Id });

        }

        public static FormattableString AppendLog(Guid id, string message) => $"UPDATE [Vidyano].[Logs]\nSET [Message] = [Message] + {message}\nWHERE ([ExternalId] = {id})";
        public static FormattableString CleanupLogs(int days) => $"delete from [Vidyano].[Logs] where [Type] <> 1 and [CreatedOn] < dateadd(d, {days}, sysdatetimeoffset())";

        public const string DeleteFromCacheUpdates = "delete from [" + schemaName + "].[CacheUpdates]";

        public const string DeleteFromRegisteredStreams = "delete from [" + schemaName + "].[RegisteredStreams] where [ValidUntil] < sysdatetimeoffset()";

        public const string PostSynchronize = "if (object_id('dbo.PostSynchronize', 'P') is not null) begin exec dbo.PostSynchronize insert into Vidyano.CacheUpdates (Id, Timestamp, Data) values(newid(), sysdatetimeoffset(), cast('' as varbinary(max))) select 1 end else select 0";

        public static FormattableString PrependLog(Guid id, string message) => $"UPDATE [Vidyano].[Logs]\nSET [Message] = {message} + [Message]\nWHERE ([ExternalId] = {id})";
        public static FormattableString RemoveUserFromGroup(Guid userId, Guid groupId) => $"delete from [Vidyano].[UserGroup] where [Users_Id] = {userId} and [Groups_id] = {groupId}";
    }
}