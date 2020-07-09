using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

using Octokit;

namespace THNETII.Octokit.FileProviders
{
    public class GitHubRepositoryFileProvider : IFileProvider
    {
        private readonly IRepositoryCommitsClient repoCommitClient;
        private readonly IRepositoryContentsClient repoContentClient;

        public string Owner { get; }
        public string Repository { get; }
        public string? Reference { get; }

        internal GitHubRepositoryFileProvider(IGitHubClient client,
            Uri repositoryUri, string? reference = null)
            : this(client,
                  GetOwnerAndRepositoryFromUri(repositoryUri ?? throw new ArgumentNullException(nameof(repositoryUri))),
                  reference)
        { }

        [SuppressMessage("Globalization", "CA1303: Do not pass literals as localized parameters")]
        private static (string owner, string name) GetOwnerAndRepositoryFromUri(
            Uri uri)
        {
            try
            {
                string owner = uri.Segments[1].TrimEnd('/').TrimEnd();
                string name = uri.Segments[2].TrimEnd('/').TrimEnd();
                const string dotGitSuffix = ".git";
                if (name.EndsWith(dotGitSuffix, StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - dotGitSuffix.Length);

                return (owner, name);
            }
            catch (IndexOutOfRangeException idxExcept)
            {
                throw new ArgumentException("Repository URL must contain at least three path segments.",
                    nameof(uri), idxExcept);
            }
        }

        [DebuggerStepThrough]
        private GitHubRepositoryFileProvider(IGitHubClient client,
            (string owner, string name) repo, string? reference = null)
            : this(client, repo.owner, repo.name, reference)
        { }

        [SuppressMessage("Globalization", "CA1303: Do not pass literals as localized parameters")]
        public GitHubRepositoryFileProvider(IGitHubClient client,
            string owner, string repository, string? reference = null)
            : base()
        {
            _ = client ?? throw new ArgumentNullException(nameof(client));
            repoContentClient = client.Repository.Content;
            repoCommitClient = client.Repository.Commit;
            Owner = owner switch
            {
                string _ when string.IsNullOrWhiteSpace(owner) =>
                    throw new ArgumentException("Repository owner must contain at least one non-whitespace character", nameof(owner)),
                null => throw new ArgumentNullException(nameof(owner)),
                _ => owner,
            };
            Repository = repository switch
            {
                string _ when string.IsNullOrWhiteSpace(repository) =>
                    throw new ArgumentException("Repository owner must contain at least one non-whitespace character", nameof(repository)),
                null => throw new ArgumentNullException(nameof(repository)),
                _ => repository,
            };
            Reference = reference;
        }

        public async Task<IDirectoryContents> GetDirectoryContentsAsync(
            string subpath, CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<IFileInfo> GetFileInfoAsync(string subpath,
            CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public IDirectoryContents GetDirectoryContents(string subpath) =>
            GetDirectoryContentsAsync(subpath)
            .ConfigureAwait(false).GetAwaiter().GetResult();

        public IFileInfo GetFileInfo(string subpath) =>
            GetFileInfoAsync(subpath)
            .ConfigureAwait(false).GetAwaiter().GetResult();

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }
}
