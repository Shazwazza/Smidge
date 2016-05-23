namespace Smidge
{
    /// <summary>
    /// Used to transform a virtual path to an absolute path
    /// </summary>
    public interface IVirtualPathTranslator
    {
        /// <summary>
        /// This will normalize the web path - synonymous with IUrlHelper.Content method but does not require IUrlHelper
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string Content(string path);
    }
}