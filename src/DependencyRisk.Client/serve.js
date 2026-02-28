const { spawn } = require('child_process');
const path = require('path');

const node = process.execPath;
const ng = path.join(__dirname, 'node_modules', '@angular', 'cli', 'bin', 'ng.js');
const proc = spawn(node, [ng, 'serve'], {
  cwd: __dirname,
  stdio: 'inherit'
});

proc.on('exit', code => process.exit(code ?? 0));
