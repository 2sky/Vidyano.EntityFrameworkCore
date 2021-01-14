using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Vidyano.Core.Extensions;
using Vidyano.Core.Services;
using Vidyano.Service.Repository;
using Vidyano.Service.Repository.DataLayer;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    /// <summary>
    /// Uses EntityFrameworkCore to get the repository data from a SQL server database.
    /// </summary>
    public class DefaultRepositoryProvider : DbContext, IRepositoryProvider
        , IRepositoryRegisteredStreamStore
        , IRepositoryLogStore
        , IRepositoryFeedbackStore
        , IRepositorySettingStore
    {
        private readonly IRepositoryCacheUpdateStore cacheUpdateStore;

        private static readonly long baseTicks = new DateTime(2000, 1, 1).Ticks;
        private bool skipUpdateVidyanoCache;

        /// <summary>
        /// Creates a new instance of the <see cref="DefaultRepositoryProvider"/> class using the default Vidyano connection string.
        /// </summary>
        public DefaultRepositoryProvider()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DefaultRepositoryProvider"/> class using the specified <paramref name="options"/>.
        /// </summary>
        public DefaultRepositoryProvider(DbContextOptions<DefaultRepositoryProvider> options, IRepositoryCacheUpdateStore cacheUpdateStore)
            : base(options)
        {
            this.cacheUpdateStore = cacheUpdateStore;
        }

        /// <summary>
        /// The [Vidyano].[CacheUpdates] table.
        /// </summary>
        public DbSet<CacheUpdateDto> CacheUpdates { get; set; }

        /// <summary>
        /// The [Vidyano].[RegisteredStreams] table.
        /// </summary>
        public DbSet<RegisteredStreamDto> RegisteredStreams { get; set; }

        /// <summary>
        /// The [Vidyano].[Feedbacks] table.
        /// </summary>
        public DbSet<FeedbackDto> Feedbacks { get; set; }

        /// <summary>
        /// The [Vidyano].[Logs] table.
        /// </summary>
        public DbSet<LogDto> Logs { get; set; }

        /// <summary>
        /// The [Vidyano].[Settings] table.
        /// </summary>
        public DbSet<SettingDto> Settings { get; set; }

        /// <summary>
        /// The [Vidyano].[Users] table.
        /// </summary>
        public DbSet<UserDto> Users { get; set; }

        /// <summary>
        /// The [Vidyano].[Groups] table.
        /// </summary>
        public DbSet<GroupDto> Groups { get; set; }

        /// <summary>
        /// The [Vidyano].[UserGroup] table.
        /// </summary>
        internal DbSet<UserGroup> UserGroups { get; set; }

        /// <summary>
        /// The [Vidyano].[UserNotifications] table.
        /// </summary>
        internal DbSet<UserNotificationDto> UserNotifications { get; set; }

        /// <summary>
        /// The [Vidyano].[UserProfiles] table.
        /// </summary>
        internal DbSet<UserProfile> UserProfiles { get; set; }

        /// <summary>
        /// The [Vidyano].[UserSettings] table.
        /// </summary>
        internal DbSet<UserSettings> UserSettings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder) => SqlStatements.OnModelCreating(modelBuilder);

        /// <inheritdoc />
        public virtual void Initialize(InitializeArgs args)
        {
            new RepositoryInitializer().Initialize(this, args);
        }

        //public virtual void DeleteExpiredCacheUpdates(DateTimeOffset expired)
        //{
        //    Retry.Do(() => Database.ExecuteSqlRaw("delete from [" + schemaName + "].[CacheUpdates] where [Timestamp] < @expired", new SqlParameter("expired", expired)));
        //}

        public virtual CacheUpdateDto[] GetRecentCacheUpdates(DateTimeOffset since)
        {
            return CacheUpdates
                .AsNoTracking()
                .OrderBy(update => update.Timestamp)
                .Where(update => update.Timestamp > since)
                .ToArray();
        }

        /// <inheritdoc />
        public virtual RegisteredStreamDto? GetRegisteredStream(Guid id, Guid userId)
        {
            // NOTE: Try locally tracked first, otherwise check database
            var result = RegisteredStreams.Local
                .FirstOrDefault(rs => rs.Id == id);
            if (result != null && result.UserId != userId)
                return null;

            if (result == null)
            {
                result = RegisteredStreams
                    .AsNoTracking()
                    .FirstOrDefault(rs => rs.Id == id);
            }

            if (result != null)
            {
                var utcNow = DateTimeOffset.UtcNow;
                if (result.ValidUntil < utcNow)
                {
                    Entry(result).State = EntityState.Detached;
                    result = null;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public virtual void PersistRegisteredStream(RegisteredStreamDto registeredStream)
        {
            ApplicationIntentDeterminer.CheckRepositoryReadOnly();

            Retry.Do(() => Database.ExecuteSqlRaw(SqlStatements.DeleteFromRegisteredStreams));
            var utcNow = DateTimeOffset.UtcNow;
            ChangeTracker
                .Entries<RegisteredStreamDto>()
                .Where(e => e.Entity.ValidUntil < utcNow)
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);

            RegisteredStreams.Add(registeredStream);

            PersistChanges();
        }

        /// <inheritdoc />
        public Source<IFeedbackDto> GetFeedbacks(Query query)
        {
            return Feedbacks
                .AsNoTracking()
                .AsSource<IFeedbackDto>();
        }

        /// <inheritdoc />
        public IFeedbackDto AddFeedback(FeedbackDto entity)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = CreateSequentialGuid();

            Feedbacks.Add(entity);
            return entity;
        }

        /// <inheritdoc />
        public virtual IFeedbackDto? GetFeedback(string objectId)
        {
            var id = objectId.FromServiceString<Guid>();
            return Feedbacks
                .AsNoTracking()
                .FirstOrDefault(f => f.Id == id);
        }

        /// <inheritdoc />
        public virtual IAttachmentDto? GetFeedbackAttachment(IFeedbackDto feedback)
        {
            if (!(feedback is FeedbackDto dto))
                throw new ArgumentException("Invalid feedback type.", nameof(feedback));

            return (FeedbackAttachment?)dto;
        }

        /// <inheritdoc />
        public virtual void RemoveFeedback(string objectId)
        {
            var id = objectId.FromServiceString<Guid>();
            var entry = Entry(new FeedbackDto { Id = id });
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Unchanged;

            entry.State = EntityState.Deleted;
        }

        /// <inheritdoc />
        public virtual Guid AddLog(string message, LogType type = LogType.Error, IUser? user = null)
        {
            var log = new LogDto();
            log.Id = CreateSequentialGuid();
            log.CreatedOn = (DateTimeOffset)DataTypes.GetDefaultValue(typeof(DateTimeOffset), DataTypes.DateTimeOffset)!;
            user ??= Manager.Current?.User;
            if (user != null)
                log.UserId = user.Id;
            log.Message = message;
            log.Type = type;

            Logs.Add(log);

            return log.Id;
        }

        /// <inheritdoc />
        public virtual Source<ILogDto> GetLogs(Query? query)
        {
            return Logs
                .AsNoTracking()
                .AsSource<ILogDto>();
        }

        /// <inheritdoc />
        public virtual ILogDto? GetLog(PersistentObject obj)
        {
            var id = obj.ObjectId.FromServiceString<Guid>();
            var projected = Logs
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    l.CreatedOn,
                    l.Type,
                    l.Message,
                    l.UserId
                })
                .FirstOrDefault();
            if (projected == null)
                return null;

            return new LogDto
            {
                Id = id,
                CreatedOn = projected.CreatedOn,
                Type = projected.Type,
                Message = projected.Message,
                UserId = projected.UserId
            };
        }

        /// <inheritdoc />
        public virtual ILogDto[] GetLogsSince(DateTimeOffset since, params LogType[] types)
        {
            var logs = Logs
                .AsNoTracking()
                .Where(l => l.CreatedOn >= since);
            if (types.Length > 0)
                logs = logs.Where(l => types.Contains(l.Type));
            return logs.ToArray<ILogDto>();
        }

        /// <inheritdoc />
        public virtual void RemoveLog(Guid id)
        {
            var entry = Entry(new LogDto { Id = id });
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Unchanged;

            entry.State = EntityState.Deleted;
        }

        /// <inheritdoc />
        public virtual void AppendLog(Guid id, string message)
        {
            Database.ExecuteSqlInterpolated(SqlStatements.AppendLog(id, message));
        }

        /// <inheritdoc />
        public virtual void PrependLog(Guid id, string message)
        {
            Database.ExecuteSqlInterpolated(SqlStatements.PrependLog(id, message));
        }

        /// <inheritdoc />
        public virtual void ChangeLog(Guid id, string? message, LogType? type)
        {
            var log = new LogDto { Id = id };
            Logs.Attach(log);

            if (message != null)
                log.Message = message;

            if (type != null)
                log.Type = type.GetValueOrDefault();

            SaveChanges();
        }

        /// <inheritdoc />
        public virtual void CleanupLogs(int days)
        {
            Retry.Do(() => Database.ExecuteSqlInterpolated(SqlStatements.CleanupLogs(-days)));
        }

        /// <inheritdoc />
        public virtual ISettingDto[] Initialize(ISettingDto[] systemSettings, string serviceVersion)
        {
            var cache = Settings.ToDictionary(s => s.Key);

            if (ApplicationIntentDeterminer.Repository == ApplicationIntent.ReadWrite)
            {
                var changed = false;

                // Handle missing settings
                var missingSettings = systemSettings.Where(s => !cache.ContainsKey(s.Key)).ToArray();
                if (missingSettings.Length > 0)
                {
                    foreach (var missingSetting in missingSettings)
                    {
                        var newSetting = new SettingDto
                        {
                            Key = missingSetting.Key,
                            Value = missingSetting.Value,
                            DataType = missingSetting.DataType,
                            Description = missingSetting.Description,
                            IsSystem = true
                        };
                        cache[missingSetting.Key] = newSetting;
                        Settings.Add(newSetting);
                    }

                    changed = true;
                }

                // Update service version
                if (cache.TryGetValue("ServiceVersion", out var versionSetting) && versionSetting.Value != serviceVersion)
                {
                    versionSetting.Value = serviceVersion;
                    changed = true;
                }

                if (changed)
                    PersistChanges();
            }

            return cache.Values.ToArray<ISettingDto>();
        }

        /// <inheritdoc />
        public virtual ISettingDto AddSetting(CreateSettingInput input)
        {
            var setting = new SettingDto
            {
                Key = input.Key,
                Value = input.Value,
                Description = input.Description,
                DataType = input.DataType,
            };
            Settings.Add(setting);
            return setting;
        }

        public virtual ISettingDto? GetSetting(string key)
        {
            return GetSettingInternal(key);
        }

        private SettingDto? GetSettingInternal(string key)
        {
            return Settings.FirstOrDefault(s => s.Key == key);
        }

        /// <inheritdoc />
        public virtual ISettingDto UpdateSetting(UpdateSettingInput input)
        {
            var setting = GetSettingInternal(input.Key);
            if (setting == null)
            {
                setting = new SettingDto
                {
                    Key = input.Key,
                };
                Settings.Add(setting);
            }
            input.ApplyTo(setting);
            return setting;
        }

        /// <inheritdoc />
        public virtual void RemoveSetting(string key)
        {
            var setting = GetSetting(key);
            if (setting != null)
                Remove(setting);
        }

        /// <inheritdoc />
        public virtual bool HasRepositoryConnection(int tries = 5, TimeSpan wait = default)
        {
            var i = 0;
            while (i++ < tries)
            {
                try
                {
                    Settings.Select(s => s.Key).Take(1).Run();
                    return true;
                }
                catch
                {
                    Thread.Sleep(wait);
                }
            }

            return false;
        }

        /// <summary>
        /// Pushes all pending changes to the underlying data store.
        /// </summary>
        public virtual void PersistChanges()
        {
            SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var changedEntries = GetChangedEntries();
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            GenerateCacheUpdates(changedEntries);
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var changedEntries = GetChangedEntries();
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
            GenerateCacheUpdates(changedEntries);
            return result;
        }

        private ObjectChanged[] GetChangedEntries()
        {
            if (ServiceLocator.GetService<IVidyanoConfiguration>().DisableCacheUpdates)
                return Empty<ObjectChanged>.Array;

            ChangeTracker.DetectChanges();
            var changedEntries = ChangeTracker.Entries()
                .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted) && (e.Entity is UserDto || e.Entity is SettingDto || e.Entity is GroupDto))
                .Select(e => new ObjectChanged(e))
                .ToArray();
            return changedEntries;
        }

        private void GenerateCacheUpdates(ObjectChanged[] changedEntries)
        {
            if (changedEntries.Length == 0)
                return;

            //TODO: use correct settings
            var updateVidyanoCache = ApplicationIntentDeterminer.Repository == ApplicationIntent.ReadWrite;
            var skipUpdateLogging = false;
            var isSingleInstance = false;

            if (updateVidyanoCache && !skipUpdateVidyanoCache)
            {
                var updateData = Empty<byte>.Array;
                var updateId = Guid.NewGuid();
                CacheUpdateChange? change = null;
                string? value = null;

                if (!skipUpdateLogging)
                {
                    if (changedEntries.Length == 1)
                    {
                        change = changedEntries[0].Change;
                        value = changedEntries[0].Value;
                    }

                    if (changedEntries.Length < 100 && !isSingleInstance)
                        updateData = CacheSynchronizer.SerializeChangedEntries(changedEntries);

                    //cacheUpdateStore.Update(changedEntries, updateId, true, change, value);
                }

                try
                {
                    skipUpdateVidyanoCache = true;

                    if (!skipUpdateLogging && !isSingleInstance)
                    {
                        var update = new CacheUpdateDto();
                        update.Id = updateId;
                        update.Data = updateData;
                        update.Change = change;
                        update.Value = value;

                        if (updateData.Length == 0)
                            CacheUpdates.RemoveRange(CacheUpdates);

                        CacheUpdates.Add(update);
                        cacheUpdateStore.AddProcessedCacheUpdate(update.Id);
                    }

                    base.SaveChanges();
                }
                finally
                {
                    skipUpdateVidyanoCache = false;
                }
            }
        }

        /// <summary>
        /// Generates a <see cref="System.Guid"/> values using a strategy suggested on Jimmy Nilsson's <a href="http://www.informit.com/articles/article.aspx?p=25862">article</a>.
        /// </summary>
        public static Guid CreateSequentialGuid()
        {
            var guidBytes = Guid.NewGuid().ToByteArray();
            var now = DateTime.UtcNow;
            var daysArray = BitConverter.GetBytes(new TimeSpan(now.Ticks - baseTicks).Days);
            var msecsArray = BitConverter.GetBytes((long)(now.TimeOfDay.TotalMilliseconds / 3.333333));
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);
            Array.Copy(daysArray, 2, guidBytes, 10, 2);
            Array.Copy(msecsArray, 4, guidBytes, 12, 4);
            return new Guid(guidBytes);
        }
    }

    public class CacheSynchronizer : IRepositoryCacheUpdateStore
    {
        private readonly IRepositoryCacheUpdates cacheUpdater;
        private DateTimeOffset lastUpdatesChecked;
        private DateTimeOffset lastCacheUpdates = DateTimeOffset.MinValue;
        private readonly HashSet<Guid> cacheUpdates = new HashSet<Guid>();
        private static readonly DataContractSerializer cacheUpdateSerializer = new DataContractSerializer(typeof(ObjectChanged[]));

        public CacheSynchronizer(IRepositoryCacheUpdates cacheUpdater)
        {
            this.cacheUpdater = cacheUpdater;
        }

        public void CheckForUpdates()
        {
            var currentChecked = lastUpdatesChecked;
            lock (cacheUpdater.CacheSyncRoot)
            {
                if (currentChecked != lastUpdatesChecked)
                    return;

                var now = DateTimeOffset.UtcNow;

                if ((now - lastCacheUpdates).TotalHours > 12d)
                {
                    lastCacheUpdates = DateTimeOffset.Now.AddMinutes(-5d); // Clock skew
                    cacheUpdater.ReloadCache();
                    lastUpdatesChecked = now;
                    return;
                }

                using var scope = ServiceLocator.GetScopedRequiredService<DefaultRepositoryProvider>(out var context);
                var updates = context
                    .GetRecentCacheUpdates(lastCacheUpdates)
                    .Where(u => !cacheUpdates.Contains(u.Id))
                    .ToArray();
                if (updates.Length > 0)
                {
                    if (updates.Any(u => u.Data.Length == 0))
                    {
                        cacheUpdater.ReloadCache();
                        return;
                    }

                    var stopUpdating = false;
                    foreach (var update in updates)
                    {
                        var data = update.Data;
                        using (var reader = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress))
                        {
                            try
                            {
                                var deserializedEntries = (ObjectChanged[])cacheUpdateSerializer.ReadObject(reader);

                                Update(deserializedEntries, update.Id, false, update.Change, update.Value);
                            }
                            catch (Exception ex)
                            {
                                var message = ServiceLocator.GetService<IExceptionService>().GenerateMessage(ex);
                                Manager.Current.Log(message, LogType.Warning);

                                stopUpdating = true;
                            }
                        }

                        cacheUpdates.Add(update.Id);

                        if (stopUpdating)
                        {
                            // NOTE: Something failed updating, so just recreate from scratch
                            cacheUpdater.ReloadCache();
                            return;
                        }
                    }

                    lastCacheUpdates = updates.Max(update => update.Timestamp);
                }
                else
                    lastCacheUpdates = now.AddMinutes(-5d); // Clock skew
            }
        }

        private void Update(ObjectChanged[] changedEntries, Guid updateId, bool fromSaveChanges, CacheUpdateChange? change, string? value)
        {
            cacheUpdates.Add(updateId);

            if (changedEntries.Length > 100)
            {
                Manager.Current.Log("Too many cache updates, reloading entire cache.", LogType.Information);
                cacheUpdater.ReloadCache();
                return;
            }

            try
            {
                //TODO: improve cache updates

                //var refreshedInstances = new HashSet<object>();

                //foreach (var entityEntry in changedEntries)
                //{
                //    if (ReadContext.TryGetObjectByKey(entityEntry.EntityKey, out var obj))
                //    {
                //        if (obj is User updatedUser && !updatedUser.IsGroup)
                //        {
                //            if (userCache.TryGetValue(updatedUser.Name, out var cachedUser))
                //            {
                //                if (updatedUser.ProfileObject[UserNotificationHandler.ProfileKey] != cachedUser.ProfileObject[UserNotificationHandler.ProfileKey]) //perhaps throttle this
                //                    UserNotificationHandler.UpdateUserNotifications(updatedUser);
                //            }
                //        }
                //        entityEntry.Entity = obj;
                //    }
                //}

                foreach (var entry in changedEntries)
                {
                    if (entry.Change == CacheUpdateChange.UserLastLoginDate)
                    {
                        cacheUpdater.UserChanged((Guid) entry.EntityKey, entry.Value.FromServiceString<DateTimeOffset>());
                    }
                    else if (entry.EntityTypeName == nameof(GroupDto))
                    {
                        if (entry.State == EntityState.Deleted)
                            cacheUpdater.GroupRemoved((Guid)entry.EntityKey);
                        else
                            cacheUpdater.GroupChanged((Guid)entry.EntityKey);
                    }
                    else if (entry.EntityTypeName == nameof(UserDto))
                    {
                        if (entry.State == EntityState.Deleted)
                            cacheUpdater.UserRemoved((Guid)entry.EntityKey);
                        else
                            cacheUpdater.UserChanged((Guid)entry.EntityKey);
                    }
                    else if (entry.EntityTypeName == nameof(SettingDto))
                    {
                        if (entry.State == EntityState.Deleted)
                            cacheUpdater.SettingRemoved((string)entry.EntityKey);
                        else
                            cacheUpdater.SettingChanged((string)entry.EntityKey);
                    }
                }
            }
            catch (Exception ex)
            {
                // NOTE: Most of the time this is just InvalidOperationException: The connection was not closed. The connection's current state is connecting.

                var sb = new StringBuilder("Cache update failed, reloading entire cache.");
                sb.AppendLine();
                sb.AppendLine("Changed entries:");
                foreach (var entry in changedEntries)
                {
                    sb.Append($"Entity {entry.EntityTypeName} ({entry.EntityKey}): {entry.State}").AppendLine();
                }

                sb.AppendLine();
                sb.AppendLine();
                sb.Append(ServiceLocator.GetService<IExceptionService>().GenerateMessage(ex));

                Manager.Current.Log(sb.ToString(), LogType.Warning);

                cacheUpdater.ReloadCache();
            }
        }

        public void AddProcessedCacheUpdate(Guid updateId)
        {
            cacheUpdates.Add(updateId);
        }

        public void CleanupUpdates()
        {

        }

        internal static byte[] SerializeChangedEntries(ObjectChanged[] changedEntries)
        {
            using (var ms = new MemoryStream())
            using (var writer = new DeflateStream(ms, CompressionMode.Compress))
            {
                cacheUpdateSerializer.WriteObject(writer, changedEntries);

                writer.Flush();
                writer.Close();
                return ms.ToArray();
            }
        }
    }

    [DataContract]
    public sealed class ObjectChanged
    {
        #region Fields

        private static readonly string[] trackedProperties =
        {
            nameof(IUser.LastLoginDate),
        };

        #endregion

        #region Constructors

        [Obsolete("For serializer only",true)]
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public ObjectChanged()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
        }

        public ObjectChanged(EntityEntry entry)
        {
            State = entry.State;
            EntityTypeName = entry.Entity.GetType().Name;
            EntityKey = entry.Metadata.FindPrimaryKey()
                    .Properties
                    .Select(p => entry.Property(p.Name).CurrentValue).Single();

            if (entry.State != EntityState.Deleted)
            {
                var modifiedProperties = entry.Properties.Where(p => p.IsModified).ToArray();
                if (modifiedProperties.Length == 1 && trackedProperties.Contains(modifiedProperties[0].Metadata.Name))
                {
                    switch (modifiedProperties[0].Metadata.Name)
                    {
                        case nameof(UserDto.LastLoginDate):
                            Change = CacheUpdateChange.UserLastLoginDate;
                            Value = ((UserDto)entry.Entity).LastLoginDate.ToServiceString();
                            break;
                        default:
                            throw new InvalidOperationException("Unmapped property: " + modifiedProperties[0]);
                    }
                }
                else
                {
                    Change = CacheUpdateChange.Multiple;
                    Value = string.Join(";", modifiedProperties.Select(m => m.Metadata.Name));
                }
            }
        }

        #endregion

        #region Properties

        [DataMember]
        public object EntityKey { get; set; }

        [DataMember]
        public string EntityTypeName { get; set; }

        [DataMember]
        public EntityState State { get; set; }

        [DataMember]
        public CacheUpdateChange? Change { get; set; }

        [DataMember]
        public string? Value { get; set; }

        #endregion

    }
}