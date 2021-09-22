using Microsoft.Extensions.FileProviders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    /// <summary>
    /// A marker interface for an <see cref="IFileProvider"/> for Smidge
    /// </summary>
    public interface ISmidgeFileProvider : IFileProvider
    {
    }
}
