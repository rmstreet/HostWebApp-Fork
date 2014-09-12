using System;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Frankstein.Common.Mvc
{
    public class CustomWebViewPage : WebViewPage
    {
        public override void Execute()
        {

        }

        public bool IsRazorWebPage
        {
            get { return false; }
        }

        public override void ExecutePageHierarchy()
        {
            using (DisposableTimer.StartNew("CustomWebViewPage: " + this.VirtualPath))
            {
                if (IsAjax || string.IsNullOrWhiteSpace(Layout))
                {
                    base.ExecutePageHierarchy();
                    return;
                }

                using (Output.BeginChunk("div", VirtualPath, false, "view"))
                {
                    base.ExecutePageHierarchy();
                }
            }
        }

        public HelperResult RenderSectionEx(string name, bool required = false)
        {
            if (IsAjax || string.IsNullOrWhiteSpace(Layout))
            {
                return RenderSection(name, required);
            }

            var result = RenderSection(name, required);

            if (result == null)
                return null;

            //encapsula o resultado da section num novo resultado
            return new HelperResult(writer =>
            {
                using (writer.BeginChunk("div", name, true, "section"))
                {
                    result.WriteTo(writer);
                }
            });
        }
    }

    public class CustomWebViewPage<T> : WebViewPage<T>
    {
        public override void Execute()
        {
            //actually this is never called
            throw new NotImplementedException();
        }

        public bool IsRazorWebPage
        {
            get { return false; }
        }

        public override void ExecutePageHierarchy()
        {
            using (DisposableTimer.StartNew("CustomWebViewPage<" + typeof(T).Name + ">: " + this.VirtualPath))
            {
                if (IsAjax || string.IsNullOrWhiteSpace(Layout))
                {
                    base.ExecutePageHierarchy();
                    return;
                }

                using (Output.BeginChunk("div", VirtualPath, false, "view"))
                {
                    base.ExecutePageHierarchy();
                }
            }
        }

        public HelperResult RenderSectionEx(string name, bool required = false)
        {
            if (IsAjax || string.IsNullOrWhiteSpace(Layout))
            {
                return RenderSection(name, required);
            }

            var result = RenderSection(name, required);


            if (result == null)
                return null;

            //encapsula o resultado da section num novo resultado
            return new HelperResult(writer =>
            {
                using (writer.BeginChunk("div", name, true, "section"))
                {
                    result.WriteTo(writer);
                }
            });
        }
    }
}