// f1.js
var smidgeFolderOne = (function () {
    var counter = 0;
    return function increment() {
        counter = counter + 1;
        return counter;
    };
})();
