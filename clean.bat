
@echo off
rd /s /q .vs

pushd .
cd OpenSilverPdfViewer

pushd .
cd OpenSilverPdfViewer
rd /s /q bin
rd /s /q obj
popd

pushd .
cd OpenSilverPdfViewer.Browser
del /q wwwroot\Data\compressed.tracemonkey-pldi-09.pdf
del /q wwwroot\Data\POH_Calidus_4.0_EN.pdf
rd /s /q bin
rd /s /q obj
popd

pushd .
cd OpenSilverPdfViewer.Simulator
rd /s /q bin
rd /s /q obj
popd

popd
