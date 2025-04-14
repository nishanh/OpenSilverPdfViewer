
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

let pdfjsLib;
let libVersion;
let cachedPdf;

// All OpenSilver interop calls must include a C# callback function as the last parameter.
// This is because OpenSilver does not support the consumption of JS promises.
// The callback function is invoked when the promise is fulfilled.
// The functions are separated into synchronous and asynchronous to avoid nesting promises for readability

function loadPdfJs(callback) {
    if (pdfjsLib == undefined) {
        var promise = (async () => await loadPdfJsAsync())();
        if (callback != undefined) {
            promise.then((result) => callback(result));
        }
    }
    else {
        console.log("loadPdfJs() already called");
        callback(pdfjsLib);
    }
}

function getLibraryVersion(callback) {
    if (libVersion == undefined) {
        console.log("logLibraryVersion() begin");
        var promise = (async () => await getLibraryVersionAsync())();
        if (callback != undefined) {
            promise.then((result) => callback(result));
        }
    }
    else {
        console.log("logLibraryVersion() already called");
        callback(libVersion);
    }
}

function loadPdfFile(pdfFilename, callback) {
    console.log("loadPdfFile() begin: ", pdfFilename);
    var promise = (async () => await loadPdfFileAsync(pdfFilename))();
    if (callback != undefined) {
        promise.then((result) => callback(result));
    }
}

function loadPdfStream(pdfFileStream, callback) {
    console.log("loadPdfStream() begin");
    var promise = (async () => await loadPdfStreamAsync(pdfFileStream))();
    if (callback != undefined) {
        promise.then((result) => callback(result));
    }
}

function renderPage(pageNumber, canvasId, callback) {
    console.log("renderPage() begin: ", pageNumber, canvasId);
    var promise = (async () => await renderPageAsync(pageNumber, canvasId))();
    if (callback != undefined) {
        promise.then((result) => callback(result));
    }
}

function getViewportSize(canvasId) {
    var canvas = document.getElementById(canvasId);
    if (canvas == null) {
        console.error("Canvas not found");
        return null;
    }
    var rect = canvas.getBoundingClientRect();
    var viewportSize = {
        width: rect.width,
        height: rect.height
    };
    console.log("getViewportSize(): ", viewportSize.width, viewportSize.height);
    return JSON.stringify(viewportSize);
}

function renderPageToViewport(pageNumber, canvasId, callback) {
    console.log("renderPageToViewport() begin: ", pageNumber, canvasId);
    var promise = (async () => await renderPageToViewportAsync(pageNumber, canvasId))();
    if (callback != undefined) {
        promise.then((result) => callback(result));
    }
}

function renderPageThumbnail(pageNumber, thumbScale, callback) {
    console.log("renderPageThumbnail() begin: ", pageNumber);
    var promise = (async () => await renderPageThumbnailAsync(pageNumber, thumbScale))();
    if (callback != undefined) {
        promise.then((result) => callback(result));
    }
}

function logToConsole(msg) {
    console.log(msg);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Internal use only. Do not call these from C# code. It won't work.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////

async function loadPdfJsAsync() {
    if (pdfjsLib == undefined) {
        pdfjsLib = await import('https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.min.mjs');
        pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.worker.min.mjs';
        console.log("PDF.js library loaded");
    }
}

async function getLibraryVersionAsync() {
    await loadPdfJsAsync();
    const version = pdfjsLib.version
    var versionFmt = `PDF.js version: ${version}`;
    console.log(versionFmt);
    libVersion = versionFmt;
    return versionFmt;
}

// Load a PDF file from a URL
async function loadPdfFileAsync(pdfFileName) {
    await loadPdfJsAsync();

    const loadingTask = pdfjsLib.getDocument(pdfFileName);
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
}

// Load a PDF file from a base64 encoded string
async function loadPdfStreamAsync(pdfFileStream) {
    await loadPdfJsAsync();

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
}

async function renderPageToViewportAsync(pageNumber, canvasId) {
    if (!this.cachedPdf) {
        console.error('No PDF loaded. Call loadPdfFile first.');
        return -1; // Indicate an error
    }
    try {
        // Set the render desired dpi.
        // Higher values will result in better quality images, but reduce performance.
        // Values that are not multiples of 72 may cause interpolation artifacts as it will be misaligned with
        // native Pdf 72pt grid.
        const dpi = 144; 
        const scale = dpi / 72.0; // Calculate the scale factor based on the native PDF DPI

        const page = await this.cachedPdf.getPage(pageNumber);
        const contentView = page.getViewport({ scale });

        // Create an unattached canvas element to render the PDF page onto
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        canvas.width = contentView.width;
        canvas.height = contentView.height;

        const renderContext = {
            canvasContext: context,
            viewport: contentView
        };
        await page.render(renderContext).promise;

        //var dataUrl = canvas.toDataURL('image/png', 100);
        //console.log("canvas.toDataURL: ", dataUrl);

        var viewportCanvas = document.getElementById(canvasId);
        var htmlPresenter = viewportCanvas.parentNode.parentNode; 
        htmlPresenter.style.overflow = "hidden"; // Hide browser default. I do my own styled scrollbars in XAML

        // Reset the default target canvas element size
        var parentRect = htmlPresenter.getBoundingClientRect();
        viewportCanvas.width = parentRect.width;
        viewportCanvas.height = parentRect.height;

        var fitToViewScale = Math.min(viewportCanvas.width / contentView.width, viewportCanvas.height / contentView.height);
        var scaledWidth = contentView.width * fitToViewScale;
        var scaledHeight = contentView.height * fitToViewScale;

        var centerX = (viewportCanvas.width - scaledWidth) / 2;
        var centerY = (viewportCanvas.height - scaledHeight) / 2;
        const scrollX = 0;
        const scrollY = 0;

        var ctx = viewportCanvas.getContext('2d');
        // ctx.drawImage(canvas, -scrollX, -scrollY, scaledWidth, scaledHeight);
        ctx.drawImage(canvas, centerX, centerY, scaledWidth, scaledHeight);

        console.log(`Page ${pageNumber} rendered successfully`);
        console.log("displayScale: ", fitToViewScale);
        console.log(`Source size: ${contentView.width} x ${contentView.height}`);
        console.log(`Viewport size: ${viewportCanvas.width} x ${viewportCanvas.height}`);
        console.log(`Scaled size: ${scaledWidth} x ${scaledHeight}`);
        console.log(`Scroll offset: X: ${scrollX}, Y: ${scrollY}`);

        return pageNumber; // Return the rendered page number
    }
    catch (error) {
        console.error(`Error rendering page ${pageNumber}:`, error);
        return -1; // Indicate an error
    }
}

async function renderPageThumbnailAsync(pageNumber, thumbScale) {
    if (!this.cachedPdf) {
        console.error('No PDF loaded. Call loadPdfFile first.');
        return null; // Indicate an error
    }
    try {
        const page = await this.cachedPdf.getPage(pageNumber);
        const contentView = page.getViewport({ scale: thumbScale });

        // Create an unattached canvas element to render the PDF page onto
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        canvas.width = contentView.width;
        canvas.height = contentView.height;

        const renderContext = {
            canvasContext: context,
            viewport: contentView
        };
        await page.render(renderContext).promise;
        var thumbData = canvas.toDataURL('image/png', 100);

        console.log(`Page ${pageNumber} thumbnail rendered successfully`);
        console.log(`Output size: ${contentView.width} x ${contentView.height}`);

        return thumbData;
    }
    catch (error) {
        console.error(`Error rendering thumbnail page ${pageNumber}:`, error);
        return null; // Indicate an error
    }
}

// Render a specific page from the cached PDF
async function renderPageAsync(pageNumber, canvasId) {
    if (!this.cachedPdf) {
        console.error('No PDF loaded. Call loadPdfFile first.');
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
        console.log(`Canvas size: ${viewport.width} x ${viewport.height}`);
        
        return pageNumber; // Return the rendered page number
    }
    catch (error) {
        console.error(`Error rendering page ${pageNumber}:`, error);
        return -1; // Indicate an error
    }
}

