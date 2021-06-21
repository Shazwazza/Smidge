using System;

namespace Smidge.CompositeFiles
{

    /// <summary>
    /// Represents group of files (composite files) and the URL to retrieve the contents from along with it's 
    /// hashed key for the set of files
    /// </summary>
    public class FileSetUrl
    {       
        public string Url { get; set; }
        public string Key { get; set; }
    }
}