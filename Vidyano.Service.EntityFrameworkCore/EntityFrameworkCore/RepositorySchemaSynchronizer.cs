using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

// ReSharper disable UnusedMember.Local
namespace Vidyano.Service.EntityFrameworkCore
{
    [Obfuscation(Feature = "renaming")]
    sealed class RepositorySchemaSynchronizer
    {
        #region Fields

        private readonly DbContext seedContext;
        private int currentVersion;

        private static readonly Dictionary<int, MethodInfo> updateMethods;
        public static readonly int Version;

        #endregion

        #region Constructors

        static RepositorySchemaSynchronizer()
        {
            updateMethods = typeof(RepositorySchemaSynchronizer).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.Name.StartsWith("UpdateToV")).ToDictionary(m => int.Parse(m.Name.Substring(9)), m => m);
            Version = updateMethods.Keys.Max();
        }

        public RepositorySchemaSynchronizer(DbContext seedContext, int currentVersion)
        {
            this.seedContext = seedContext;
            this.currentVersion = currentVersion;
        }

        #endregion

        #region Properties

        public static int? NewerVersion { get; set; }

        #endregion

        #region Public Methods

        public void Synchronize()
        {
            if (currentVersion > Version)
                throw new FaultException("Service is connected to repository with newer schema.");

            if (currentVersion < 45)
                throw new InvalidOperationException("Repository is too old, expecting at least 45, got " + currentVersion);

            ExecuteSql("delete from [Vidyano].[CacheUpdates]");

            while (currentVersion < Version)
            {
                currentVersion++;

                if (!updateMethods.TryGetValue(currentVersion, out var method))
                    throw new InvalidOperationException(string.Format("Could not find update method for version {0}.", currentVersion));

                ((Action)Delegate.CreateDelegate(typeof(Action), this, method))();
            }
        }

        #endregion

        #region Private Methods

        private void ExecuteSql(string sql)
        {
            seedContext.Database.ExecuteSqlRaw(sql);
        }

        private void UpdateToV61()
        {
            // Vidyano.Upgrade tool will update to V61

            throw new FaultException("Run upgrade tool on project first.");
        }

        // NOTE: Also update VidyanoEntityModel.edmx.sql
        /*
         * NOTE: SQL Scripts need to be forward compatible:
         * - New tables are okay (references should use cascade delete)
         * - New columns should be null or have a default constraint
         * - Limitations should be gracefull (e.g. maxlength, ...)
         * - DO NOT rename columns or drop tables
         */

        #endregion
    }
}