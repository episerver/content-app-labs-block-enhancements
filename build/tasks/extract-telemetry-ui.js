"use strict";

const gulp = require("gulp"),
    path = require("path"),
    glob = require("glob"),
    fs = require("fs"),
    unzip = require("gulp-unzip"),
    gutil = require("gulp-util");

module.exports = (dtkHelper) => {

    gulp.task("extract-telemetry-ui", (done) => {
        // Resolve the path to the dtk zip file.
        const telemetryZip = path.resolve(glob.sync("packages/EPiServer.Telemetry.UI/*/contentFiles/any/any/modules/_protected/episerver-telemetry-ui/episerver-telemetry-ui.zip")[0]);
        const destination = dtkHelper.dtkPath + "\\episerver-telemetry-ui";

        fs.stat(destination, (err, stat) => {
            if (stat && stat.isDirectory()) {
                gutil.log(gutil.colors.cyan(telemetryZip, "has already been decompressed to: ", destination));
                done();
                return;
            }

            gulp.src(telemetryZip)
                .pipe(unzip())
                .pipe(gulp.dest(destination))
                .on("end", () => {
                    done();
                });
        });
    });
};
