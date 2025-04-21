
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

let pdfjsLib;
let libVersion;
let pdfDocument;
let pageCache = new Map(); // Cache for PDF pages

// All OpenSilver asynchronous interop calls must include a C# callback function as the last parameter.
// This is because OpenSilver does not support the consumption of JS promises.
// The callback function is invoked when the promise is fulfilled.
// The functions are separated into synchronous and asynchronous to avoid nesting promises for readability

// Before you purists complain, I know this is not the best practice and that I could be using nested 'then's
// but I think that the IIFE approach that allows async/await syntax is more readable and maintainable.

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
        console.log("getLibraryVersion() begin");
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

function getLibraryVersion2(callback) {
    if (libVersion == undefined) {
        console.log("getLibraryVersion2() begin");
        loadPdfJsAsync2().then((pdfLib) => {
            if (callback != undefined) {
                pdfjsLib = pdfLib;
                libVersion = `PDF.js version: ${pdfjsLib.version}`
                callback(libVersion);
            }
        });
    }
    else {
        console.log("logLibraryVersion() already called");
        callback(libVersion);
    }
}

function getLogicalPageSize(pageNumber, callback) {
    var promise = (async () => await getPageSizeAsync(pageNumber))();
    if (callback != undefined) {
        promise.then((result) => {
            var pageSizeJson = JSON.stringify(result);
            callback(pageSizeJson)
        });
    }
}

function getDevicePageSize(pageNumber) {
    var sourceImagecanvas = pageCache.get(pageNumber);
    if (sourceImagecanvas == undefined) {
        console.error("Page image not found in cache");
        return;
    }
    var pageSize = {
        width: sourceImagecanvas.width,
        height: sourceImagecanvas.height
    };
    return JSON.stringify(pageSize);
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

function renderPageToViewport(pageNumber, dpi, zoomLevel, canvasId, callback) {
    console.log("renderPageToViewport() begin: ", pageNumber, canvasId);
    var promise = (async () => await renderPageToViewportAsync(pageNumber, dpi, zoomLevel, canvasId))();
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

function renderPageToBlob(pageNumber, thumbScale, callback) {
    console.log("renderPageToBlob() begin: ", pageNumber);
    var promise = (async () => await renderPageToBlobAsync(pageNumber, thumbScale))();
    if (callback != undefined) {
        promise.then((result) => callback(result));
    }
}
function renderPageToImage(pageNumber, thumbScale, callback) {
    console.log("renderPageToImage() begin: ", pageNumber);
    var promise = (async () => await renderPageToImageAsync(pageNumber, thumbScale))();
    if (callback != undefined) {
        promise.then((result) => callback(result));
    }
}

// Redraws the page image to the target canvas at the specified scale and offsets to simulate viewport scrolling
function scrollViewportImage(pageNumber, canvasId, zoomLevel, scrollX, scrollY) {
    var sourceCanvas = pageCache.get(pageNumber);
    if (sourceCanvas == undefined) {
        console.error("Page image not found in cache");
        return; 
    }

    var destinationCanvas = document.getElementById(canvasId);
    var viewportRect = destinationCanvas.getBoundingClientRect();
    var displayScale = zoomLevel == 0 ? // zoomLevel == 0 means fit to viewport
        Math.min(viewportRect.width / sourceCanvas.width, viewportRect.height / sourceCanvas.height) :
        zoomLevel / 100;

    var scaledWidth = sourceCanvas.width * displayScale;
    var scaledHeight = sourceCanvas.height * displayScale;
    var sourceX = scrollX / displayScale;
    var sourceY = scrollY / displayScale;

    var ctx = destinationCanvas.getContext('2d');
    ctx.clearRect(0, 0, viewportRect.width, viewportRect.height);

    // Draw only a sub-rect of the source to the destination
    ctx.drawImage(sourceCanvas,
        sourceX, sourceY,
        sourceCanvas.width - sourceX,
        sourceCanvas.height - sourceY,
        0, 0,
        scaledWidth - scrollX,
        scaledHeight - scrollY);

    console.log(`Scroll offset: X: ${scrollX}, Y: ${scrollY}`);
}

function clearViewport(canvasId) {
    var viewportCanvas = document.getElementById(canvasId);
    var viewportRect = viewportCanvas.getBoundingClientRect();
    var ctx = viewportCanvas.getContext('2d');
    ctx.clearRect(0, 0, viewportRect.width, viewportRect.height);
}

function invalidatePageCache() {
    console.log("invalidatePageCache() begin");
    pageCache.clear(); // Clear the cache
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

// Happy now?
function loadPdfJsAsync2() {
    var promise = new Promise(resolve => {
        if (pdfjsLib == undefined) {
            import('https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.min.mjs').then((pdfLib) => {
                pdfLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.worker.min.mjs';
                resolve(pdfLib);
            });
        }
        else {
            console.log("PDF.js library already loaded");
            resolve(pdfjsLib);
        }
    });
    return promise;
}

async function getLibraryVersionAsync() {
    await loadPdfJsAsync();
    const version = pdfjsLib.version
    var versionFmt = `PDF.js version: ${version}`;
    console.log(versionFmt);
    libVersion = versionFmt;
    return versionFmt;
}

// Returns the size of the page in points (1/72 inch)
async function getPageSizeAsync(pageNumber) {
    if (!this.pdfDocument) {
        console.error('No PDF loaded. Call loadPdfFile first.');
        return -1; 
    }
    const page = await this.pdfDocument.getPage(pageNumber);
    const viewport = page.getViewport({ scale: 1.0 });
    return { width: viewport.width, height: viewport.height };
}

// Load a PDF file from a URL
async function loadPdfFileAsync(pdfFileName) {
    await loadPdfJsAsync();

    const loadingTask = pdfjsLib.getDocument(pdfFileName);
    try {
        const pdf = await loadingTask.promise;
        console.log(`PDF loaded with ${pdf.numPages} pages`);
        this.pdfDocument = pdf; // Cache the PDF object
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
    const bytes = Uint8Array.from(binaryString, char => char.charCodeAt(0));
    const loadingTask = pdfjsLib.getDocument(bytes);

    try {
        const pdf = await loadingTask.promise;
        console.log(`PDF loaded with ${pdf.numPages} pages`);
        this.pdfDocument = pdf; // Cache the PDF object
        return pdf.numPages; // Return the number of pages
    }
    catch (error) {
        console.error('Error loading PDF:', error);
        return -1; // Return -1 to indicate an error
    }
}

// First renders the page to an offscreen canvas and then draws it to the target canvas at scale
// Will attempt to retrieve the page image from the cache if it has already been rendered
async function renderPageToViewportAsync(pageNumber, dpi, zoomLevel, canvasId) {
    if (!this.pdfDocument) {
        console.error('No PDF loaded. Call loadPdfFile first.');
        return -1; // Indicate an error
    }
    try {
        // Higher dpi values will result in better quality images, but reduce performance.
        // Values that are not multiples of 72 may cause interpolation artifacts as it will be misaligned with the native Pdf 72pt grid.
        const sourceScale = dpi / 72.0; // Calculate the scale factor based on the native PDF DPI

        var canvas = pageCache.get(pageNumber);
        if (canvas == undefined) {
            const page = await this.pdfDocument.getPage(pageNumber);
            var contentView = page.getViewport({ scale: sourceScale });

            // Create an unattached canvas element to render the PDF page onto
            canvas = new OffscreenCanvas(contentView.width, contentView.height);
            const context = canvas.getContext('2d');

            const renderContext = {
                canvasContext: context,
                viewport: contentView
            };
            await page.render(renderContext).promise;
            pageCache.set(pageNumber, canvas); // Cache the rendered page
        }
        else {
            console.log(`Page ${pageNumber} retrieved from cache`);
        }

        var viewportCanvas = document.getElementById(canvasId);
        var htmlPresenter = viewportCanvas.parentNode.parentNode; 
        htmlPresenter.style.overflow = "hidden"; // Hide browser default. I do my own styled scrollbars in XAML

        // Reset the default target canvas element size
        var parentRect = htmlPresenter.getBoundingClientRect();
        viewportCanvas.width = parentRect.width;
        viewportCanvas.height = parentRect.height;

        var scale = zoomLevel == 0 ? // zoomLevel == 0 means fit to viewport
            Math.min(viewportCanvas.width / canvas.width, viewportCanvas.height / canvas.height) :
            zoomLevel / 100;

        var scaledWidth = canvas.width * scale;
        var scaledHeight = canvas.height * scale;

        var posX = zoomLevel == 0 ? (viewportCanvas.width - scaledWidth) / 2 : 0;
        var posY = zoomLevel == 0 ? (viewportCanvas.height - scaledHeight) / 2 : 0;

        var ctx = viewportCanvas.getContext('2d');
        ctx.clearRect(0, 0, viewportCanvas.width, viewportCanvas.height); 
        ctx.drawImage(canvas, posX, posY, scaledWidth, scaledHeight);

        console.log(`Page ${pageNumber} rendered successfully`);
        console.log("displayScale: ", scale);
        console.log(`Source size: ${canvas.width} x ${canvas.height}`);
        console.log(`Viewport size: ${viewportCanvas.width} x ${viewportCanvas.height}`);
        console.log(`Scaled size: ${scaledWidth} x ${scaledHeight}`);

        return pageNumber; // Return the rendered page number
    }
    catch (error) {
        console.error(`Error rendering page ${pageNumber}:`, error);
        return -1; // Indicate an error
    }
}

// Renders the page to an offscreen canvas and returns the image data stream as a string.
// Using scale factors that generate large images is not recommended as it will cause the browser to exceed its memory limits.
async function renderPageThumbnailAsync(pageNumber, thumbScale) {
    if (!this.pdfDocument) {
        console.error('No PDF loaded. Call loadPdfFile first.');
        return null; // Indicate an error
    }
    try {
        const page = await this.pdfDocument.getPage(pageNumber);
        const contentView = page.getViewport({ scale: thumbScale });

        // Create an unattached canvas element to render the PDF page onto
        const canvas = document.createElement('canvas');
        canvas.width = contentView.width;
        canvas.height = contentView.height;

        // new OffscreenCanvas(contentView.width, contentView.height); // No toDataURL function on OffscreenCanvas
        const context = canvas.getContext('2d');

        const renderContext = {
            canvasContext: context,
            viewport: contentView
        };
        await page.render(renderContext).promise;
        var sizeHeader = `${contentView.width}:${contentView.height};`;
        var thumbData = sizeHeader.concat(canvas.toDataURL('image/png', 100));

        console.log(`Page ${pageNumber} thumbnail rendered successfully`);
        console.log(`Output size: ${contentView.width} x ${contentView.height}`);

        return thumbData;
    }
    catch (error) {
        console.error(`Error rendering thumbnail page ${pageNumber}:`, error);
        return null; // Indicate an error
    }
}

async function renderPageToBlobAsync(pageNumber, thumbScale) {
    if (!this.pdfDocument) {
        console.error('No PDF loaded. Call loadPdfFile first.');
        return null; // Indicate an error
    }
    try {
        const page = await this.pdfDocument.getPage(pageNumber);
        const contentView = page.getViewport({ scale: thumbScale });

        // Create an unattached canvas element to render the PDF page onto
        const canvas = document.createElement('canvas');
        canvas.width = contentView.width;
        canvas.height = contentView.height;

        // new OffscreenCanvas(contentView.width, contentView.height); // No toDataURL function on OffscreenCanvas
        const context = canvas.getContext('2d');

        const renderContext = {
            canvasContext: context,
            viewport: contentView
        };
        await page.render(renderContext).promise;

        var blobUrl = await new Promise(resolve => {
            canvas.toBlob((blob) => {
                const url = URL.createObjectURL(blob);
                resolve(url);
            })
        });

        console.log("Blob: ", blobUrl);
        console.log(`Page ${pageNumber} thumbnail rendered successfully`);
        console.log(`Output size: ${contentView.width} x ${contentView.height}`);

        var sizeHeader = `${contentView.width}:${contentView.height};`;
        return sizeHeader.concat(blobUrl);
    }
    catch (error) {
        console.error(`Error rendering thumbnail page ${pageNumber}:`, error);
        return null; // Indicate an error
    }
}

async function renderPageToImageAsync(pageNumber, thumbScale) {
    if (!this.pdfDocument) {
        console.error('No PDF loaded. Call loadPdfFile first.');
        return null; // Indicate an error
    }
    try {
        const page = await this.pdfDocument.getPage(pageNumber);
        const contentView = page.getViewport({ scale: thumbScale });

        // Create an unattached canvas element to render the PDF page onto
        const canvas = document.createElement('canvas');
        canvas.width = contentView.width;
        canvas.height = contentView.height;

        // new OffscreenCanvas(contentView.width, contentView.height); // No toDataURL function on OffscreenCanvas
        const context = canvas.getContext('2d');

        const renderContext = {
            canvasContext: context,
            viewport: contentView
        };
        await page.render(renderContext).promise;

        var image = await new Promise(resolve => {
            canvas.toBlob((blob) => {
                const url = URL.createObjectURL(blob);
                const imgElement = new Image();
                imgElement.src = url;
                imgElement.onload = () => resolve(imgElement);
            })
        });

        return image;
    }
    catch (error) {
        console.error(`Error rendering thumbnail page ${pageNumber}:`, error);
        return null; // Indicate an error
    }
}
function loadBlobImageAsync(imgElement, blobUri, callback) {
    new Promise((resolve, reject) => {
        imgElement.src = blobUri;
        imgElement.onload = () => resolve(true);
        imgElement.onerror = () => resolve(false);
    }).then(result => callback(result))
}

