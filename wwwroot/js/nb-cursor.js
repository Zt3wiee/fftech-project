// Custom cursor: a dot that follows the mouse and a ring that trails behind it.
// Styles are in css/novabridge.css ("Custom cursor").
(function () {
  // Only for a real mouse, and not for people who asked for less motion.
  var finePointer = window.matchMedia('(hover: hover) and (pointer: fine)').matches;
  var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (!finePointer || reduceMotion) return;

  var root = document.documentElement;
  var dot = document.createElement('div');
  var ring = document.createElement('div');
  dot.className = 'nb-cursor-dot';
  ring.className = 'nb-cursor-ring';
  dot.setAttribute('aria-hidden', 'true');
  ring.setAttribute('aria-hidden', 'true');
  document.body.appendChild(ring);
  document.body.appendChild(dot);

  // Things that make the ring grow when hovered.
  var hoverTargets = 'a, button, [role="button"], label, .theme-btn, ' +
    '.nb-industry-card, .nb-work-card, .nb-feature, .vl-service-icon-box-4';

  var mouseX = -100, mouseY = -100;
  var ringX = -100, ringY = -100;
  var started = false;

  document.addEventListener('mousemove', function (e) {
    mouseX = e.clientX;
    mouseY = e.clientY;
    dot.style.transform = 'translate(' + mouseX + 'px, ' + mouseY + 'px)';
    if (!started) {
      // First move: jump the ring to the mouse and show the cursor.
      started = true;
      ringX = mouseX;
      ringY = mouseY;
      root.classList.add('nb-cursor-on');
      requestAnimationFrame(follow);
    }
    root.classList.remove('nb-cursor-out');
  });

  // The ring eases towards the mouse each frame, which gives the trailing effect.
  function follow() {
    ringX += (mouseX - ringX) * 0.18;
    ringY += (mouseY - ringY) * 0.18;
    ring.style.transform = 'translate(' + ringX + 'px, ' + ringY + 'px)';
    requestAnimationFrame(follow);
  }

  document.addEventListener('mouseover', function (e) {
    root.classList.toggle('nb-cursor-hover', !!e.target.closest(hoverTargets));
  });
  document.addEventListener('mousedown', function () { root.classList.add('nb-cursor-down'); });
  document.addEventListener('mouseup', function () { root.classList.remove('nb-cursor-down'); });
  document.documentElement.addEventListener('mouseleave', function () { root.classList.add('nb-cursor-out'); });
})();
