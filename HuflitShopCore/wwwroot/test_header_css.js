const fs = require('fs');
const content = fs.readFileSync('d:\\WEB\\HuflitShopCore\\HuflitShopCore\\wwwroot\\Bean\\main.scss.css', 'utf8');

const regex = /header(?:\.header)?\s*\{([^{}]+)\}/g;
let match;
console.log("Searching for basic header rules in main.scss.css:");
while ((match = regex.exec(content)) !== null) {
    console.log(match[0]);
}

const regex2 = /\.header\s*\{([^{}]+)\}/g;
while ((match = regex2.exec(content)) !== null) {
    console.log(match[0]);
}
