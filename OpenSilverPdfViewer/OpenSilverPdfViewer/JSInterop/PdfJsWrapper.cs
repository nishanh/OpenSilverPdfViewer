using System.Threading.Tasks;

namespace OpenSilverPdfViewer.JSInterop
{
    public class PdfJsWrapper
    {
        private const string scriptResourceName = "/OpenSilverPdfViewer;component/JSInterop/pdfJsInterop.js";
        private static PdfJsWrapper _instance;
        public static PdfJsWrapper Interop => _instance ?? (_instance = new PdfJsWrapper());

        public string Version { get; private set; }

        private PdfJsWrapper() { }
        public async Task Init()
        {
            if (string.IsNullOrEmpty(Version))
            {
                await OpenSilver.Interop.LoadJavaScriptFile(scriptResourceName);
                Version = await JSAsyncTaskRunner.RunJavaScriptAsync<string>("getLibraryVersion");
            }
        }
        public async Task<int> LoadPdfFile(string fileName)
        {
            await Init();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("loadPdfFile", fileName);
        }
        public async Task<int> RenderPage(int pageNumber, string canvasId)
        {
            await Init();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("renderPage", pageNumber, canvasId);
        }
        public async Task<int> RenderPageToViewport(int pageNumber, string canvasId)
        {
            await Init();
            return await JSAsyncTaskRunner.RunJavaScriptAsync<int>("renderPageToViewport", pageNumber, canvasId);
        }
    }
}
