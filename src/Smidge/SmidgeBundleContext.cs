using System;
using Smidge.Models;

namespace Smidge
{
    internal class SmidgeBundleContext : ISmidgeRequire
    {
        private readonly string _bundleName;
        private readonly BundleManager _bundleManager;
        private readonly WebFileType _type;

        public SmidgeBundleContext(string bundleName, BundleManager bundleManager, WebFileType type)
        {
            _bundleName = bundleName;
            _bundleManager = bundleManager;
            _type = type;
        }

        public ISmidgeRequire RequiresJs(JavaScriptFile file)
        {
            if (_type == WebFileType.Css)
                throw new InvalidOperationException("Cannot add css file to a js bundle");
            if (FileSystemHelper.IsExternalRequestPath(file.FilePath))
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");

            _bundleManager.AddToBundle(_bundleName, file);
            return this;
        }

        public ISmidgeRequire RequiresJs(params string[] paths)
        {
            if (_type == WebFileType.Css)
                throw new InvalidOperationException("Cannot add css file to a js bundle");

            foreach (var path in paths)
            {
                if (FileSystemHelper.IsExternalRequestPath(path))
                    throw new InvalidOperationException("Cannot process an external file as part of a bundle");
                _bundleManager.AddToBundle(_bundleName, new JavaScriptFile(path));
            }
            return this;
        }

        public ISmidgeRequire RequiresCss(CssFile file)
        {
            if (_type == WebFileType.Js)
                throw new InvalidOperationException("Cannot add js file to a css bundle");
            if (FileSystemHelper.IsExternalRequestPath(file.FilePath))
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            _bundleManager.AddToBundle(_bundleName, file);
            return this;
        }

        public ISmidgeRequire RequiresCss(params string[] paths)
        {
            if (_type == WebFileType.Js)
                throw new InvalidOperationException("Cannot add js file to a css bundle");

            foreach (var path in paths)
            {
                if (FileSystemHelper.IsExternalRequestPath(path))
                    throw new InvalidOperationException("Cannot process an external file as part of a bundle");
                _bundleManager.AddToBundle(_bundleName, new CssFile(path));
            }
            return this;
        }
    }
}