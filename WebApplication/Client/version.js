const fs = require('fs'); 
fs.writeFile(
	"./src/version.ts",
	"export class Version { public static commit:string = \"" + process.argv[2] + "\"; public static branch:string = \"" + process.argv[3] + "\"; }", 
	function(){}
); 