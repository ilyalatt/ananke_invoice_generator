var system = require('system'), fs = require('fs');

var input = system.args[1];
var output = system.args[2];

var page = require('webpage').create();
page.paperSize = {
    format: 'A4',
    orientation: 'portrait'
};
page.settings.dpi = '96';

page.open(input, function (status) {
    if (status !== 'success') {
        console.log('unable to load the input');
        phantom.exit(1);
        return;
    }
    window.setTimeout(function () {
        page.render(output);
        phantom.exit();
    }, 0);
});
