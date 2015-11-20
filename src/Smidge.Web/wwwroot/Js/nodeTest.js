var grunt = require('grunt');

module.exports = function (callback) {

    var writeStuff = "not written";

    grunt.registerTask('test', 'Log some stuff.', function () {
        writeStuff = "WHOOHOO!";
    });

    grunt.tasks(['test']);

    callback(null, writeStuff);
}
