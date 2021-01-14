using System;
using System.Linq;
using Vidyano.Service.Repository;
using Vidyano.Service.Repository.DataLayer;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    public class DefaultRepositoryUserNotificationStore : IRepositoryUserNotificationStore
    {
        private readonly DefaultRepositoryProvider context;

        public DefaultRepositoryUserNotificationStore(DefaultRepositoryProvider context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public virtual IUserNotificationDto NewUserNotification()
        {
            return new UserNotificationDto
            {
                Id = DefaultRepositoryProvider.CreateSequentialGuid()
            };
        }

        /// <inheritdoc />
        public virtual void AddUserNotification(IUserNotificationDto userNotification)
        {
            context.UserNotifications.Add((UserNotificationDto)userNotification);
        }

        /// <inheritdoc />
        public virtual IUserNotificationDto? GetUserNotification(string objectId)
        {
            var id = objectId.FromServiceString<Guid>();
            return context.UserNotifications.Find(id);
        }

        /// <inheritdoc />
        public virtual IUserNotificationDto[] GetUserNotifications(string[] objectIds)
        {
            var ids = objectIds.Select(objectId => objectId.FromServiceString<Guid>()).ToArray();
            return context.UserNotifications.Where(n => ids.Contains(n.Id)).ToArray<IUserNotificationDto>();
        }

        /// <inheritdoc />
        public virtual Source<IUserNotificationDto> GetUserNotifications(Query? query = null)
        {
            return context.UserNotifications.AsSource<IUserNotificationDto>();
        }

        /// <inheritdoc />
        public virtual void PersistChanges()
        {
            context.SaveChanges();
        }
    }
}