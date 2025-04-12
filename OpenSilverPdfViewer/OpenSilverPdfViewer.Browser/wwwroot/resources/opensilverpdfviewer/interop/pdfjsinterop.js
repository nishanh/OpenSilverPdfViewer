//window.pdfJsInterop = {
pdfJsInterop = {
    cachedPdf: null, // Store the loaded PDF object

    logLibraryVersion: async function () {
        const pdfjsLib = await import('https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.min.mjs');
        const version = pdfjsLib.version
        var version = `PDF.js version: ${version}`;
        console.log(version);
        return version;
    },
    // Load a PDF file from a URL
    loadPdfFile: async function (pdfFileName) {
        const pdfjsLib = await import('https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.min.mjs');
        pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.worker.min.mjs';

        const pdfUrl = `/data/${pdfFileName}`;
        const loadingTask = pdfjsLib.getDocument(pdfUrl);

        try {
            const pdf = await loadingTask.promise;
            console.log(`PDF loaded with ${pdf.numPages} pages`);
            this.cachedPdf = pdf; // Cache the PDF object
            return pdf.numPages; // Return the number of pages
        }
        catch (error) {
            console.error('Error loading PDF:', error);
            return -1; // Return -1 to indicate an error
        }
    },

    // Load a PDF file from a base64 encoded string
    loadPdfStream: async function (pdfFileStream) {
        const pdfjsLib = await import('https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.min.mjs');
        pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.worker.min.mjs';

        const binaryString = atob(pdfFileStream);
        const len = binaryString.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        const loadingTask = pdfjsLib.getDocument(bytes);

        try {
            const pdf = await loadingTask.promise;
            console.log(`PDF loaded with ${pdf.numPages} pages`);
            this.cachedPdf = pdf; // Cache the PDF object
            return pdf.numPages; // Return the number of pages
        }
        catch (error) {
            console.error('Error loading PDF:', error);
            return -1; // Return -1 to indicate an error
        }
    },

    // Render a specific page from the cached PDF
    renderPage: async function (pageNumber, canvasId) {
        if (!this.cachedPdf) {
            console.error('No PDF loaded. Call loadPdf first.');
            return -1; // Indicate an error
        }

        try {
            const dpi = 96; // Set the desired DPI (dots per inch)
            const scale = dpi / 72; // Calculate the scale factor based on the native PDF DPI

            const page = await this.cachedPdf.getPage(pageNumber);
            const viewport = page.getViewport({ scale });
            const canvas = document.getElementById(canvasId);
            const context = canvas.getContext('2d');
            canvas.width = viewport.width;
            canvas.height = viewport.height;

            const renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            await page.render(renderContext).promise;
            console.log(`Page ${pageNumber} rendered successfully`);
            return pageNumber; // Return the rendered page number
        }
        catch (error) {
            console.error(`Error rendering page ${pageNumber}:`, error);
            return -1; // Indicate an error
        }
    }
}