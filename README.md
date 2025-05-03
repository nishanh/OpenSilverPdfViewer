# OpenSilver Pdf Viewer Demo

This application demonstrates using Mozilla's **PDF.JS** client-side Pdf rasterizer with **Open Silver 3.2**.

Several rendering techniques are demonstrated using an embedded HTML canvas wrapped in a **HTMLPresenter**, the high-performance **HTMLCanvas** element and the legacy **Canvas** element. Each has pros and cons in terms of ease-of-implementation and performance.

A couple of other features were developed to solve some of the issues encountered along the way.

- **JSAsyncTaskRunner**. This static class allows interop calls to asynchronous Javascript functions to be made "awaitable" on the C# side using the familiar async/await pattern. You will need to *wrap* the calls on the Javascript side to accept a callback parameter and then invoke the target Javascript in a deferred approach.

- **BlobElement**. This is a new **HTMLCanvas** child element that allows in-memory images using a **Blob** Url as a *Source* to be used with **HTMLCanvas**.

- DataURL sourced **Image** content for the legacy **Canvas** object. Essentially a Base64 string representing the PDF page in PNG format used to display in-memory PDF page images.

## Known Issues

- Since all Pdf loading/processing happens on the **client**, you should avoid using any memory-intensive or excessively high page count documents with this demo. Doing so will choke the browser into non-responsiveness or crash it altogether. PDF.JS is meant to stream Pdf data from a server which is absent here and partial loading is not an option on the client-side.

- Using the system file dialog within the app after launching the application into a secondary browser tab will cause Kestrel to abort. This happens even when cancelling the dialog. This is an **OpenSilver** issue.

- **HTMLCanvs** renderer with **BlobElement** content has poor performance. Especially noticeable in Thumbnail view-mode. I'm sure this is my fault somewhere.

- Repeatedly acquiring Javascript **CanvasContext2d** objects is expensive in terms of performance. More work needs to be done in this area.

- Scroll performance is reduced when rulers are displayed. These need to be updated to use the higher performance **HTMLCanvas** object

- Back-buffer rendering using an **OffscreenCanvas** object should be implemented to suppress some of the annoying page display *flashing* issues in certain scenarios.

- Background worker thread rendering tasks cannot be awaited in the browser environment. For now, rendering task objects are marshalled back to main thread for synchronization until I develop a better approach

- As this is primarily a proof-of-concept *tech demo* for a larger application I have planned, it is somewhat light on error-checking and bullet-proofing.

- Be wary of using any of this code in a production environment. Prior experience with **PDF.JS** has demonstrated issues with some content on complicated PDFs to **not be rendered**. This was a show-stopper for the production printing industry I was working in. Mozilla blamed this on "hardware issues" and subsequently dismissed it. This was back in the 2.x version days of **PDF.JS** and this project uses v5.0.375. I don't have my legacy test-case PDFs so I cannot verify if these issues still exist.

- Only tested with the **OpenSilver** simulator, **Microsoft Edge** and **Brave** browsers.

This project uses **OpenSilver 3.2** and **.NET 9 / .NET Standard 2.0**. No additional Nuget packages are used. 

