let pdfjsLib;
let libVersion;

// ALL OpenSilver interop calls must include a C# callback function as the last parameter.
// This is because OpenSilver does not support the consumption of JS promises.
// The callback function is invoked when the promise is fulfilled.

// OpenSilver call-site
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

// OpenSilver call-site
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
function logToConsole(msg) {
    console.log(msg);
}

// DO NOT call these from C# code. It won't work.

async function loadPdfJsAsync() {
    pdfjsLib = await import('https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.min.mjs');
    pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@5.0.375/build/pdf.worker.min.mjs';
}

async function getLibraryVersionAsync() {
    await loadPdfJsAsync();
    const version = pdfjsLib.version
    var versionFmt = `PDF.js version: ${version}`;
    console.log(versionFmt);
    libVersion = versionFmt;
    return versionFmt;
}
