using System;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

using Octokit;

namespace THNETII.Octokit.FileProviders
{
    public class GitHubRepositoryFileProvider : IFileProvider
    {
        private readonly IRepositoryContentsClient client;

        public GitHubRepositoryFileProvider(IConnection connection)
            : this(new ApiConnection(connection ?? throw new ArgumentNullException(nameof(connection))))
        { }

        public GitHubRepositoryFileProvider(IApiConnection connection)
            : this(new RepositoryContentsClient(connection ?? throw new ArgumentNullException(nameof(connection))))
        { }

        public GitHubRepositoryFileProvider(IRepositoryContentsClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            throw new NotImplementedException();
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }
}
