module.exports = function (grunt) {
    'use strict';
    grunt.loadNpmTasks('grunt-exec');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-clean');
    grunt.loadNpmTasks('grunt-xmlstoke');

    grunt.registerTask("FabActUtil", ["copy:FabActUtilBackup", "copy:FabActUtil", "exec:FabActUtil","xmlstoke:FabActUtil","clean:FabActUtil"]);

    var relativePackageDir = "../../packages/";
    var buildDir = "bin/x64/Debug/";
    var temp = "artifacts"
    var assemblyName = "S-Innovations.Azure.MessageProcessor.ServiceFabric.exe";
    var relativeAppDir = "../MessageProcessor.ServiceFabricHost/";
    var appManifest = relativeAppDir + 'ApplicationPackageRoot/ApplicationManifest.xml';
    var files = {};
    files[appManifest] = appManifest;

    grunt.initConfig({
        copy:{
            FabActUtil:{
                src: relativePackageDir + "Microsoft.ServiceFabric.Actors*/build/FabActUtil.exe",
                dest: buildDir+"FabActUtil.exe"
            },
            FabActUtilBackup:{
                src:appManifest,
                dest: appManifest+".bac"
            }
        },
        clean:{
            FabActUtil: ["bin/x64/Debug/FabActUtil.exe"],
        },
        exec: {
            // Run tsd link to add bower/npm packages typescript definition files.
            "FabActUtil": {
                cmd: 'bin\\x64\\Debug\\FabActUtil.exe /app:' + relativeAppDir.replace(new RegExp('/', 'g'), "\\") + '\\ApplicationPackageRoot /out:' + temp.replace(new RegExp('/', 'g'), "\\") + ' /spp:PackageRoot /in:' +buildDir.replace(new RegExp('/', 'g'),"\\")+ assemblyName,
            }
        },
        xmlstoke: {
            FabActUtil: {
                options: {
                    actions: [{
                        xpath: '//DefaultServices/Service[@Name="QueueListenerActorService"]',
                        type: 'D'
                    }],
                },
                files: files,
            },
        },
    })
}
