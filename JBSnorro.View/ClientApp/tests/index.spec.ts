// import '../dist/tests/test.spec.js';
// var glob = require("glob");
// var fs = require('fs');

// require all .spec.js files

// glob("**/*.spec.js", null, function (er: Error | null, files: string[]) {
//   files.forEach(function (file: string) { require(file); });
// })

// the above gives the following error:
// TypeError: fs.readdir is not a function

// function importTestFile(name: string, path: string) {
//   describe(name, function () {
//     console.log(path);
//     require(path);
//   });
// }

// describe('top', function () {

//   //  importTestFile('0', './test.spec.js');
//   //  importTestFile('1', './test.spec.1.js');
// });

import 'mocha';

describe('Hello function', () => {

  it('should return hello world', () => {

    require('./test.spec');
    require('./test.spec.1');
    console.warn('e');
  });

});