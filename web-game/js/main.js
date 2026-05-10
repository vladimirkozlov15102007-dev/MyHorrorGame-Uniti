import { Game } from './game.js';
import { audio } from './audio.js';

const menu = document.getElementById('menu');
const hud = document.getElementById('hud');
const loader = document.getElementById('loader');
const startBtn = document.getElementById('startBtn');

let game = null;

startBtn.addEventListener('click', async () => {
  menu.classList.add('hidden');
  loader.classList.remove('hidden');
  await audio.start();
  // yield so CSS loader paints
  await new Promise(r => setTimeout(r, 50));
  try {
    game = new Game();
  } catch (e) {
    console.error(e);
    loader.innerHTML = `<div style="color:#c14b4b;padding:40px;max-width:600px;font-family:monospace">
      Failed to initialize: ${e.message}<br>${e.stack}</div>`;
    return;
  }
  loader.classList.add('hidden');
  hud.classList.remove('hidden');
  game.start();
});

document.addEventListener('pointerlockchange', () => {
  if (!document.pointerLockElement && game && game.running && game.gameState === 'playing') {
    // auto-re-lock on click
    document.addEventListener('click', () => {
      if (game && game.running) document.getElementById('gl').requestPointerLock();
    }, { once: true });
  }
});

// lock on click to resume
document.getElementById('gl').addEventListener('click', () => {
  if (game && game.running) document.getElementById('gl').requestPointerLock();
});
