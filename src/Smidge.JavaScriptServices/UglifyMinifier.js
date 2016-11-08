var UglifyJS = require("uglify-js");

module.exports = function (callback, fileContent) {
    var result = UglifyJS.minify(fileContent, {fromString: true});
    callback(null, result.code);
}