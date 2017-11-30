﻿using System;
using HgVersion.Configuration;
using HgVersion.VCS;
using Mercurial;
using VCSVersion;
using VCSVersion.Configuration;
using VCSVersion.Helpers;
using VCSVersion.VCS;

namespace HgVersion
{
    /// <inheritdoc />
    public class HgVersionContext : IVersionContext
    {
        /// <inheritdoc />
        public IRepository Repository { get; }

        /// <inheritdoc />
        public IFileSystem FileSystem { get; }
        
        /// <inheritdoc />
        public Config FullConfiguration { get; }

        /// <inheritdoc />
        public EffectiveConfiguration Configuration { get; }

        /// <inheritdoc />
        public IBranchHead CurrentBranch { get; }

        /// <inheritdoc />
        public ICommit CurrentCommit { get; }

        /// <inheritdoc />
        public IRepositoryMetadataProvider RepositoryMetadataProvider { get; }

        /// <summary>
        /// Creates an instance of <see cref="HgVersionContext"/>
        /// </summary>
        /// <param name="repository">Mercurial.Net <see cref="Mercurial.Repository"/></param>
        public HgVersionContext(Repository repository)
        {
            var preparer = new HgPreparer(repository.Path);
            var hg = new HgRepository(repository);

            Repository = hg;
            CurrentBranch = hg.CurrentBranch();
            CurrentCommit = hg.CurrentCommit();
            FileSystem = new FileSystem();
            FullConfiguration = HgConfigurationProvider.Provide(preparer, FileSystem);
            RepositoryMetadataProvider = new HgRepositoryMetadataProvider(hg, FullConfiguration);
            Configuration = CalculateEffectiveConfiguration();
        }

        private EffectiveConfiguration CalculateEffectiveConfiguration()
        {
            var currentBranchConfig = BranchConfigurationCalculator.GetBranchConfiguration(this, CurrentBranch);

            if (!currentBranchConfig.VersioningMode.HasValue)
                throw new Exception($"Configuration value for 'Versioning mode' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.Increment.HasValue)
                throw new Exception($"Configuration value for 'Increment' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.PreventIncrementOfMergedBranchVersion.HasValue)
                throw new Exception($"Configuration value for 'PreventIncrementOfMergedBranchVersion' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.TrackMergeTarget.HasValue)
                throw new Exception($"Configuration value for 'TrackMergeTarget' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.TracksReleaseBranches.HasValue)
                throw new Exception($"Configuration value for 'TracksReleaseBranches' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.IsReleaseBranch.HasValue)
                throw new Exception($"Configuration value for 'IsReleaseBranch' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");

            if (!FullConfiguration.AssemblyVersioningScheme.HasValue)
                throw new Exception("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.AssemblyFileVersioningScheme.HasValue)
                throw new Exception("Configuration value for 'AssemblyFileVersioningScheme' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.CommitMessageIncrementing.HasValue)
                throw new Exception("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.LegacySemVerPadding.HasValue)
                throw new Exception("Configuration value for 'LegacySemVerPadding' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.BuildMetaDataPadding.HasValue)
                throw new Exception("Configuration value for 'BuildMetaDataPadding' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.CommitsSinceVersionSourcePadding.HasValue)
                throw new Exception("Configuration value for 'CommitsSinceVersionSourcePadding' has no value. (this should not happen, please report an issue)");

            var versioningMode = currentBranchConfig.VersioningMode.Value;
            var tag = currentBranchConfig.Tag;
            var tagNumberPattern = currentBranchConfig.TagNumberPattern;
            var incrementStrategy = currentBranchConfig.Increment.Value;
            var preventIncrementForMergedBranchVersion = currentBranchConfig.PreventIncrementOfMergedBranchVersion.Value;
            var trackMergeTarget = currentBranchConfig.TrackMergeTarget.Value;

            var nextVersion = FullConfiguration.NextVersion;
            var assemblyVersioningScheme = FullConfiguration.AssemblyVersioningScheme.Value;
            var assemblyFileVersioningScheme = FullConfiguration.AssemblyFileVersioningScheme.Value;
            var assemblyInformationalFormat = FullConfiguration.AssemblyInformationalFormat;
            var gitTagPrefix = FullConfiguration.TagPrefix;
            var majorMessage = FullConfiguration.MajorVersionBumpMessage;
            var minorMessage = FullConfiguration.MinorVersionBumpMessage;
            var patchMessage = FullConfiguration.PatchVersionBumpMessage;
            var noBumpMessage = FullConfiguration.NoBumpMessage;
            var commitDateFormat = FullConfiguration.CommitDateFormat;

            var commitMessageVersionBump = currentBranchConfig.CommitMessageIncrementing ?? FullConfiguration.CommitMessageIncrementing.Value;

            return new EffectiveConfiguration(
                assemblyVersioningScheme, assemblyFileVersioningScheme, assemblyInformationalFormat, versioningMode, gitTagPrefix,
                tag, nextVersion, incrementStrategy,
                currentBranchConfig.Regex,
                preventIncrementForMergedBranchVersion,
                tagNumberPattern, FullConfiguration.ContinuousDeploymentFallbackTag,
                trackMergeTarget,
                majorMessage, minorMessage, patchMessage, noBumpMessage,
                commitMessageVersionBump,
                FullConfiguration.LegacySemVerPadding.Value,
                FullConfiguration.BuildMetaDataPadding.Value,
                FullConfiguration.CommitsSinceVersionSourcePadding.Value,
                FullConfiguration.Ignore.ToFilters(),
                currentBranchConfig.TracksReleaseBranches.Value,
                currentBranchConfig.IsReleaseBranch.Value,
                commitDateFormat);
        }
    }
}