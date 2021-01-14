using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Vidyano.Service.Repository.DataLayer;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    public class DefaultRepositoryUserStore : BaseRepositoryUserStore<UserDto, GroupDto>
    {
        private readonly DefaultRepositoryProvider context;

        public DefaultRepositoryUserStore(DefaultRepositoryProvider context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public override IUserDto[] GetUsers()
        {
            return context.Users.ToArray<IUserDto>();
        }

        /// <inheritdoc />
        public override void AddUser(UserDto user)
        {
            context.Users.Add(user);
        }

        /// <inheritdoc />
        public override IUserDto? GetUser(Guid id)
        {
            return context.Users.Find(id);
        }

        /// <inheritdoc />
        public override void RemoveUser(Guid id)
        {
            WithUser(id, u => // TODO: NETCORE: Don't load entity?
            {
                context.Users.Remove(u);
                var userGroups = context.UserGroups.Where(ug => ug.Users_Id == id).ToArray();
                context.UserGroups.RemoveRange(userGroups);
            });
        }

        /// <inheritdoc />
        public override bool IsUserMemberOf(Guid userId, Guid groupId)
        {
            return context.UserGroups.Any(ug => ug.Users_Id == userId && ug.Groups_Id == groupId);
        }

        /// <inheritdoc />
        public override void AddUserToGroup(Guid userId, Guid groupId)
        {
            context.UserGroups.Add(new UserGroup { Users_Id = userId, Groups_Id = groupId }); // TODO: NETCORE: As SQL? Does not handle duplicates correctly
        }

        /// <inheritdoc />
        public override void RemoveUserFromGroup(Guid userId, Guid groupId)
        {
            context.Database.ExecuteSqlInterpolated(SqlStatements.RemoveUserFromGroup(userId, groupId));
        }

        /// <inheritdoc />
        public override Guid[] GetGroupsForUser(Guid userId)
        {
            return context.UserGroups.Where(ug => ug.Users_Id == userId).Select(ug => ug.Groups_Id).ToArray();
        }

        /// <inheritdoc />
        public override Guid[] GetUsersForGroup(Guid groupId)
        {
            return context.UserGroups.Where(ug => ug.Groups_Id == groupId).Select(ug => ug.Users_Id).ToArray();
        }

        /// <inheritdoc />
        public override IGroupDto[] GetGroups()
        {
            return context.Groups.ToArray<IGroupDto>();
        }

        /// <inheritdoc />
        public override void AddGroup(GroupDto group)
        {
            context.Groups.Add(group);
        }

        /// <inheritdoc />
        public override IGroupDto? GetGroup(Guid id)
        {
            return context.Groups.Find(id);
        }

        /// <inheritdoc />
        public override void RemoveGroup(Guid id)
        {
            WithGroup(id, g => context.Groups.Remove(g)); // TODO: NETCORE: Without loading?
        }

        /// <inheritdoc />
        public override void SetUserSettings(Guid id, JObject settings)
        {
            var userSetting = context.UserSettings.Find(id);
            if (userSetting == null)
                context.UserSettings.Add(userSetting = new UserSettings { Id = id });
            userSetting.Settings = settings.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <inheritdoc />
        public override (Guid id, JObject settings)[] GetUserSettings()
        {
            return context.UserSettings.AsEnumerable().Select(s => (s.Id, JObject.Parse(s.Settings))).ToArray();
        }

        /// <inheritdoc />
        public override JObject? GetUserSettings(Guid id)
        {
            var userSetting = context.UserSettings.Find(id);
            return userSetting != null ? JObject.Parse(userSetting.Settings) : null;
        }

        /// <inheritdoc />
        public override void SetUserProfile(Guid id, JObject profile)
        {
            var userProfile = context.UserProfiles.Find(id);
            if (userProfile == null)
                context.UserProfiles.Add(userProfile = new UserProfile { Id = id });
            userProfile.Profile = profile.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <inheritdoc />
        public override JObject? GetUserProfile(Guid id)
        {
            var userProfile = context.UserProfiles.Find(id);
            return userProfile != null ? JObject.Parse(userProfile.Profile) : null;
        }

        /// <inheritdoc />
        public override void PersistChanges()
        {
            context.SaveChanges();
        }
    }
}