// Language switcher in the header: opens on click, closes on a click outside or Esc.
// Choosing a language also remembers it in a cookie, so the homepage opens in that language next time.
(function () {
  var COOKIE = 'site_lang';

  document.querySelectorAll('a[data-lang]').forEach(function (link) {
    link.addEventListener('click', function () {
      document.cookie = COOKIE + '=' + link.getAttribute('data-lang') + '; path=/; max-age=31536000; samesite=lax';
    });
  });

  var switcher = document.querySelector('[data-nb-lang]');
  if (!switcher) return;
  var toggle = switcher.querySelector('.nb-lang-toggle');

  function setOpen(open) {
    switcher.classList.toggle('open', open);
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
  }

  toggle.addEventListener('click', function () {
    setOpen(!switcher.classList.contains('open'));
  });

  document.addEventListener('click', function (e) {
    if (!switcher.contains(e.target)) setOpen(false);
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && switcher.classList.contains('open')) {
      setOpen(false);
      toggle.focus();
    }
  });

  // Keyboard users tabbing out of the list close it too.
  switcher.addEventListener('focusout', function (e) {
    if (!switcher.contains(e.relatedTarget)) setOpen(false);
  });
})();
