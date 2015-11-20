module.exports = function (grunt) {

    // Default task.
    grunt.registerTask('default', ['uglify']);
     
    // Project configuration.
    grunt.initConfig({
        
        uglify: {
            my_target: {
                files: {
                    'dest/output.min.js': ['src/input1.js', 'src/input2.js']
                }
            }
        }
    });

    grunt.loadNpmTasks('grunt-contrib-uglify');    
};
