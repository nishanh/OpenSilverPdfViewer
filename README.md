# OpenSilver Pdf Viewer Demo

This application demonstrates using Mozilla's PDF.JS client-side Pdf rasterizer with **Open Silver 3.2**.

Several rendering techniques are demonstrated using and embedded HTML canvas wrapped in a **HTMLPresenter**, the high-performance **HTMLCanvas** element and the **Canvas** element. Each has pros and cons in terms of ease-of-use and performance.

A couple of other features were developed to solve some of the issues encountered along the way.

- **JSAsyncTaskRunner**. This static class allows interop calls to asynchronous Javascript functions to be made "awaitable" on the C# side using the familiar async/await pattern. You will need to *wrap* the calls on the Javascript side to accept a callback parameter and then invoke the target Javascript in a deferred approach.

- **BlobElement**. This is a new **HTMLCanvas** child element that allows in-memory images using a **Blob** Url as a *Source* to be used with **HTMLCanvas**.

## Known Issues

- Acquiring Javascript **CanvasContext2d** objects is expensive in terms of performance. More work needs to be done in this area.

- Back-buffer rendering using an **OffscreenCanvas** object should be implemented to suppress some of the annoying page display *flashing* issues in certain scenarios.

- As this is primarily a proof-of-concept *tech demo* for a larger application I have planned, it is somewhat light on error-checking and bullet-proofing.

- Only tested with the **OpenSilver** simulator, **Microsoft Edge** and **Brave** browsers.

- Be wary of using any of this code in a production environment. Prior experience with **PDF.JS** has demonstrated issues with some content on complicated PDFs to *not be rendered*. This was blamed on "hardware issues" by Mozilla and subsequently dismissed. This was back in the 2.x version days of **PDF.JS** and this project uses v5.0.375. I don't have my test-cases from those days, so I cannot verify if these issues still exist.

This project uses **OpenSilver 3.2** and **.NET 9**. No additional Nuget packages are used. 

