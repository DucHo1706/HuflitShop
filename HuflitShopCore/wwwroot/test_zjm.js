const fs = require('fs');
const content = fs.readFileSync('d:\\WEB\\HuflitShopCore\\HuflitShopCore\\wwwroot\\Bean\\ZJMZ4D9Ka40.css', 'utf8');

const regex = /([^{};]+)\{[^{}]+(?:margin|padding|top|transform|position)[^:]*:[^}]+main-header[^}]*\}/g;
let match;
console.log("Searching in ZJMZ4D9Ka40.css:");
let idx = 0;
while ((idx = content.indexOf('main-header', idx)) >= 0) {
    console.log("Match: " + content.substring(Math.max(0, idx - 100), idx + 200));
    idx += 11;
}
