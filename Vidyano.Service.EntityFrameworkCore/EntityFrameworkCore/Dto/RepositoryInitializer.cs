using System;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Vidyano.Core.Services;
using Vidyano.Service.Repository.DataLayer;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    public class RepositoryInitializer
    {
        public void Initialize(DefaultRepositoryProvider seedContext, InitializeArgs args)
        {
            // EnsureRepositoryIsSeeded
            var hasChanges = false;
            bool allowChanges;

            try
            {
                allowChanges = Convert.ToBoolean(seedContext.Settings.Where(s => s.Key == "AllowAutomaticRepositoryUpgrades").Select(s => s.Value).FirstOrDefault() ?? "True");
            }
            catch (Exception ex)
            {
                // NOTE: Assume Repository database is not seeded so try to generate everything
                if (ex.Message.Contains("Vidyano.Settings") || (ex.InnerException != null && ex.InnerException.Message.Contains("Vidyano.Settings")))
                {
                    using var transaction = seedContext.Database.BeginTransaction();
                    try
                    {
                        foreach (var script in Resources.RepositorySql.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0))
                            seedContext.Database.ExecuteSqlRaw(script);

                        seedContext.SaveChanges();

                        seedContext.Database.ExecuteSqlRaw("drop table if exists \"Vidyano\".\"Initialize\";");

                        hasChanges = true;

                        transaction.Commit();
                    }
                    catch (Exception initEx)
                    {
                        ServiceLocator.GetService<IExceptionService>().Log(initEx);

                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                            // Ignore exception on rollback
                        }

                        throw;
                    }
                    finally
                    {
                        allowChanges = true;
                    }
                }
                else
                    throw;
            }

            if (UpdateDbSchema(seedContext, allowChanges))
                hasChanges = true;

            if (RepositorySchemaSynchronizer.NewerVersion == null)
            {
                EnsureRepositoryVersionIsCorrect(seedContext, args);

                var currentVersion = seedContext.Settings.Where(s => s.Key == "ServiceVersion").Select(s => s.Value).FirstOrDefault();
                if (args.UpdateMetadata(allowChanges, currentVersion))
                    hasChanges = true;
            }

            if (hasChanges)
                seedContext.SaveChanges();

            args.EnsureTargetSchemaIsSynchronized();
        }

        private static bool UpdateDbSchema(DefaultRepositoryProvider seedContext, bool allowChanges)
        {
            var result = seedContext.Settings.Where(s => s.Key == "RepositoryVersion").Select(s => s.Value).FirstOrDefault();
            if (result != null)
            {
                var currentVersion = Convert.ToInt32(result);
                var newVersion = RepositorySchemaSynchronizer.Version;
                if (currentVersion != newVersion)
                {
                    if (currentVersion > newVersion)
                    {
                        RepositorySchemaSynchronizer.NewerVersion = currentVersion;
                        return false;
                    }

                    if (!allowChanges)
                        throw new FaultException("Vidyano Repository is locked, enable the AllowAutomaticRepositoryUpgrades setting to allow automatic upgrades on this Repository.");

                    try
                    {
                        new RepositorySchemaSynchronizer(seedContext, currentVersion).Synchronize();
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException("Failed upgrading repository schema (" + currentVersion + " to " + newVersion + "):\n" + ex.Message, ex);
                    }

                    return true;
                }
            }

            return false;
        }

        private static void EnsureRepositoryVersionIsCorrect(DefaultRepositoryProvider seedContext, InitializeArgs args)
        {
            var versionSetting = seedContext.Settings.FirstOrDefault(s => s.Key == "RepositoryVersion");
            if (versionSetting == null)
            {
                versionSetting = new SettingDto();
                versionSetting.Key = "RepositoryVersion";
                versionSetting.IsSystem = true;
                seedContext.Settings.Add(versionSetting);
            }

            var newVersion = args.SchemaVersion.ToString(CultureInfo.InvariantCulture);
            if (!Equals(versionSetting.Value, newVersion))
                versionSetting.Value = newVersion;

            seedContext.SaveChanges();
        }
    }
}