using System;
using System.Linq;
using Smidge.Models;

namespace Smidge
{
    internal class SmidgeRequire : ISmidgeRequire
    {
        private readonly string _bundleName;
        private readonly IBundleManager _bundleManager;
        private readonly WebFileType _type;
        private readonly IRequestHelper _requestHelper;

        public SmidgeRequire(string bundleName, IBundleManager bundleManager, WebFileType type, IRequestHelper requestHelper)
        {
            if (bundleName == null) throw new ArgumentNullException(nameof(bundleName));
            if (bundleManager == null) throw new ArgumentNullException(nameof(bundleManager));
            if (requestHelper == null) throw new ArgumentNullException(nameof(requestHelper));
            _bundleName = bundleName;
            _bundleManager = bundleManager;
            _type = type;
            _requestHelper = requestHelper;
        }

        public ISmidgeRequire RequiresJs(JavaScriptFile file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (_type == WebFileType.Css)
            {
                throw new InvalidOperationException("Cannot add css file to a js bundle");
            }

            if (_requestHelper.IsExternalRequestPath(file.FilePath))
            {
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            }

            _bundleManager.AddToBundle(_bundleName, file);
            return this;
        }

        public ISmidgeRequire RequiresJs(params string[] paths)
        {
            if (_type == WebFileType.Css)
            {
                throw new InvalidOperationException("Cannot add css file to a js bundle");
            }

            foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                if (_requestHelper.IsExternalRequestPath(path))
                {
                    throw new InvalidOperationException("Cannot process an external file as part of a bundle");
                }

                _bundleManager.AddToBundle(_bundleName, new JavaScriptFile(path));
            }
            return this;
        }

        public ISmidgeRequire RequiresCss(CssFile file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (_type == WebFileType.Js)
            {
                throw new InvalidOperationException("Cannot add js file to a css bundle");
            }

            if (_requestHelper.IsExternalRequestPath(file.FilePath))
            {
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            }

            _bundleManager.AddToBundle(_bundleName, file);
            return this;
        }

        public ISmidgeRequire RequiresCss(params string[] paths)
        {
            if (_type == WebFileType.Js)
            {
                throw new InvalidOperationException("Cannot add js file to a css bundle");
            }

            foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                if (_requestHelper.IsExternalRequestPath(path))
                {
                    throw new InvalidOperationException("Cannot process an external file as part of a bundle");
                }

                _bundleManager.AddToBundle(_bundleName, new CssFile(path));
            }
            return this;
        }
    }
}
