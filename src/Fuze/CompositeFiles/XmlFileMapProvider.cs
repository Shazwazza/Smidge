using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Runtime;
using Fuze.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Fuze.CompositeFiles
{
    /// <summary>
    /// Creates an XML file to map a saved composite file to the URL requested for the 
    /// dependency handler. 
    /// This is used in order to determine which individual files are dependant on what composite file so 
    /// a user can remove it to clear the cache, and also if the cache expires but the file still exists
    /// this allows the system to simply read the one file again instead of compiling all of the other files
    /// into one again.
    /// </summary>
    public class XmlFileMapProvider : IFileMapProvider
    {

        public const string DefaultName = "XmlFileMap";

        private const string MapFileName = "map.xml";

        private XDocument _doc;
        private FileInfo _xmlFile;

        //private const string FileMapVirtualFolderDefault = "~/App_Data/ClientDependency";
        private static readonly object Locker = new object();

        /// <summary>
        /// Specifies the default folder to store the file map in, this allows for dynamically changing the folder on startup
        /// </summary>
        private string _fileMapFolder;
        private DirectoryInfo _xmlMapFolder;
        private IApplicationEnvironment _hostingEnvironment;
        private FuzeConfig _config;

        public XmlFileMapProvider(IApplicationEnvironment env, FuzeConfig config)
        {
            if (env == null) throw new ArgumentNullException(nameof(env));
            if (config == null) throw new ArgumentNullException(nameof(config));

            _config = config;
            _hostingEnvironment = env;
            _fileMapFolder = _config.DataFolder;

            //_fileSystem = new PhysicalFileSystem(env.WebRoot);

            _xmlMapFolder = new DirectoryInfo(Path.Combine(env.ApplicationBasePath, _fileMapFolder));

            //Name the map file according to the machine name
            _xmlFile = new FileInfo(GetXmlMapPath());

            EnsureXmlFile();

            lock (Locker)
            {
                try
                {
                    using (var stream = File.OpenRead(_xmlFile.FullName))
                    using (var reader = new StreamReader(stream))
                    {
                        _doc = XDocument.Load(reader);
                    }                        
                }
                catch (XmlException)
                {
                    //if it's an xml exception, create a new one and try one more time... should always work.
                    CreateNewXmlFile();
                    using (var stream = File.OpenRead(_xmlFile.FullName))
                    using (var reader = new StreamReader(stream))
                    {
                        _doc = XDocument.Load(_xmlFile.FullName);
                    }                        
                }
            }
        }

        /// <summary>
        /// Returns the composite file map associated with the file key, the version and the compression type
        /// </summary>
        /// <param name="fileKey"></param>
        /// <param name="version"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        public CompositeFileMap GetCompositeFile(string fileKey, int version, string compression)
        {
            if (string.IsNullOrEmpty(fileKey)) throw new ArgumentNullException("fileKey");

            EnsureXmlFile();

            var x = FindItem(fileKey, version, compression);
            try
            {
                return (x == null ? null : new CompositeFileMap(fileKey,
                    (string)x.Attribute("compression"),
                    (string)x.Attribute("file"),
                    x.Descendants("file")
                        .Select(f => ((string)f.Attribute("name"))).ToArray(),
                        int.Parse((string)x.Attribute("version"))));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Retreives the dependent file paths for the filekey/version (regardless of compression)
        /// </summary>
        /// <param name="fileKey"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public IEnumerable<string> GetDependentFiles(string fileKey, int version)
        {
            if (string.IsNullOrEmpty(fileKey)) throw new ArgumentNullException("fileKey");

            var x = FindItem(fileKey, version);
            try
            {
                if (x != null)
                {
                    var file = new CompositeFileMap(fileKey,
                                                    (string)x.Attribute("compression"),
                                                    (string)x.Attribute("file"),
                                                    x.Descendants("file")
                                                        .Select(f => ((string)f.Attribute("name"))).ToArray(),
                                                    int.Parse((string)x.Attribute("version")));
                    return file.DependentFiles;
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// Creates a new file map and file key for the dependent file list, this is used to create URLs with CompositeUrlType.MappedId 
        /// </summary>
        ///<example>
        /// <![CDATA[
        /// <map>
        ///		<item key="123xsy" 
        ///			file=""
        ///			compresion="deflate"
        ///         version="1234">
        ///			<files>
        ///				<file name="C:\asdf\JS\jquery.js" />
        ///				<file name="C:\asdf\JS\jquery.ui.js" />		
        ///			</files>
        ///		</item>
        /// </map>
        /// ]]>
        /// </example>
        public string CreateNewMap(HttpContext http,
            IEnumerable<IWebFile> dependentFiles,
            int version)
        {
            if (http == null) throw new ArgumentNullException("http");

            var builder = new StringBuilder();
            foreach (var d in dependentFiles)
            {
                builder.Append(d.FilePath);
                builder.Append(";");
            }
            var combinedFiles = builder.ToString();
            combinedFiles = combinedFiles.TrimEnd(new[] { ';' });

            var fileKey = (combinedFiles + version).GenerateHash();

            var x = FindItem(fileKey, version);

            //if no map exists, create one
            if (x == null)
            {
                //now, create a map with the file key so that it can be filled out later with the actual composite file that is created by the handler
                CreateUpdateMap(fileKey,
                    string.Empty,
                    dependentFiles,
                    string.Empty,
                    version);
            }

            return fileKey;
        }

        /// <summary>
        /// Adds/Updates an entry to the file map with the key specified, the version and dependent files listed with a map
        /// to the composite file created for the files.
        /// </summary>
        /// <param name="fileKey"></param>
        ///<param name="compressionType"></param>
        ///<param name="dependentFiles"></param>
        /// <param name="compositeFile"></param>
        ///<param name="version"></param>
        ///<example>
        /// <![CDATA[
        /// <map>
        ///		<item key="XSDFSDKJHLKSDIOUEYWCDCDSDOIUPOIUEROIJDSFHG" 
        ///			file="C:\asdf\App_Data\ClientDependency\123456.cdj"
        ///			compresion="deflate"
        ///         version="1234">
        ///			<files>
        ///				<file name="C:\asdf\JS\jquery.js" />
        ///				<file name="C:\asdf\JS\jquery.ui.js" />		
        ///			</files>
        ///		</item>
        /// </map>
        /// ]]>
        /// </example>
        public void CreateUpdateMap(string fileKey,
            string compressionType,
            IEnumerable<IWebFile> dependentFiles,
            string compositeFile,
            int version)
        {
            if (string.IsNullOrEmpty(fileKey)) throw new ArgumentNullException("fileKey");

            lock (Locker)
            {
                //see if we can find an item with the key/version/compression that exists
                var x = FindItem(fileKey, version, compressionType);

                if (x != null)
                {
                    x.Attribute("file").Value = compositeFile;
                    //remove all of the files so we can re-add them.
                    x.Element("files").Remove();

                    x.Add(CreateFileNode(dependentFiles));
                }
                else
                {
                    //if it doesn't exist, create it
                    _doc.Root.Add(new XElement("item",
                        new XAttribute("key", fileKey),
                        new XAttribute("file", compositeFile),
                        new XAttribute("compression", compressionType),
                        new XAttribute("version", version),
                        CreateFileNode(dependentFiles)));
                }

                //ensure folder exists
                Directory.CreateDirectory(_xmlFile.DirectoryName);
                using (var stream = File.OpenWrite(_xmlFile.FullName))
                using (var writer = new StreamWriter(stream))
                {
                    _doc.Save(writer);
                }
                    
            }
        }

        /// <summary>
        /// Finds an element in the map matching the key and version/compression
        /// </summary>
        /// <param name="key"></param>
        /// <param name="version"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        private XElement FindItem(string key, int version, string compression)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var items = _doc.Root.Elements("item")
                .Where(e => (string)e.Attribute("key") == key
                            && (string)e.Attribute("version") == version.ToString());
            return items.Where(e => (string)e.Attribute("compression") == compression).SingleOrDefault();
        }

        /// <summary>
        /// Finds a element in the map matching key/version
        /// </summary>
        /// <param name="key"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private XElement FindItem(string key, int version)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var items = _doc.Root.Elements("item")
                .Where(e => (string)e.Attribute("key") == key
                            && (string)e.Attribute("version") == version.ToString());
            return items.FirstOrDefault();
        }

        private XElement CreateFileNode(IEnumerable<IWebFile> dependentFiles)
        {
            var x = new XElement("files");

            //add all of the files
            foreach (var d in dependentFiles)
            {
                x.Add(new XElement("file", new XAttribute("name", d.FilePath)));
            }

            return x;
        }

        /// <summary>
        /// Returns the full path the map xml file for the current machine and install folder.
        /// </summary>
        /// <remarks>
        /// We need to create the map based on the combination of both machine name and install folder because
        /// this deals with issues for load balanced environments and file locking and also 
        /// deals with issues when the ClientDependency folder is deployed between environments
        /// since you would want your staging ClientDependencies in your live and vice versa.
        /// This is however based on the theory that each website you have will have a unique combination
        /// of folder path and machine name.
        /// </remarks>
        /// <returns></returns>
        private string GetXmlMapPath()
        {
            var folder = _xmlMapFolder.FullName;
            var folderHash = folder.GenerateHash();
            return Path.Combine(folder, _config.ServerName + "-" + folderHash + "-" + MapFileName);
        }

        private void CreateNewXmlFile()
        {
            if (File.Exists(_xmlFile.FullName))
            {
                File.Delete(_xmlFile.FullName);
            }

            if (_doc == null)
            {
                _doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                                new XElement("map"));
                Directory.CreateDirectory(_xmlFile.DirectoryName);

                using (var stream = File.OpenWrite(_xmlFile.FullName))
                using (var writer = new StreamWriter(stream))
                {
                    _doc.Save(writer);
                }
                
            }
            else
            {
                //if there's xml in memory, then the file has been deleted so write out the file
                Directory.CreateDirectory(_xmlFile.DirectoryName);
                using (var stream = File.OpenWrite(_xmlFile.FullName))
                using (var writer = new StreamWriter(stream))
                {
                    _doc.Save(writer);
                }                    
            }

        }

        private void EnsureXmlFile()
        {
            if (!File.Exists(_xmlFile.FullName))
            {
                lock (Locker)
                {
                    //double check
                    if (!File.Exists(_xmlFile.FullName))
                    {
                        if (!_xmlMapFolder.Exists)
                            _xmlMapFolder.Create();
                        CreateNewXmlFile();
                    }
                }
            }
        }        
        
    }
}