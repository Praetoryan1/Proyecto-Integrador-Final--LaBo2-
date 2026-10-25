"use strict";
document.querySelectorAll('[data-gif-toggle]').forEach(button => {
  button.addEventListener('click', () => {
    const playing = button.getAttribute('aria-pressed') === 'true';
    const img = document.getElementById(button.dataset.target);
    img.src = playing ? button.dataset.preview : button.dataset.source;
    button.setAttribute('aria-pressed', String(!playing));
    button.textContent = playing ? 'Reproducir GIF' : 'Detener GIF';
  });
});
document.querySelectorAll('[data-youtube-id]').forEach(button => {
  button.addEventListener('click', () => {
    const id = button.dataset.youtubeId;
    if (!/^[A-Za-z0-9_-]{11}$/.test(id)) return;
    const iframe = document.createElement('iframe');
    iframe.src = `https://www.youtube-nocookie.com/embed/${id}`;
    iframe.title = 'Reproductor de YouTube';
    iframe.allow = 'encrypted-media; fullscreen; picture-in-picture';
    iframe.allowFullscreen = true;
    iframe.referrerPolicy = 'strict-origin-when-cross-origin';
    button.closest('[data-youtube-container]').replaceWith(iframe);
  });
});
