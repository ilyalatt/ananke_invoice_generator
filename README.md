## ananke invoice generator

Simple Harvest pdf invoice console generator. It is mostly configurable via `config.yaml` and `invoice_template/index.html`. It uses phantom js to convert html to pdf, so you need to [download a phantom js executable](http://phantomjs.org/download.html) and place it in `phantom_js/(platform)_phantomjs.exe`, where `(platform)` can be linux, macos or windows.

Default invoice template is a bit modified [Sparksuite invoice template](https://github.com/sparksuite/simple-html-invoice-template).

The generator written in F# and .NET Core 2.0.
