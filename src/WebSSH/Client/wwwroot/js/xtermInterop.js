// Resilient xterm interop: supports delayed loading / CDN failure fallback
window.websshTerm = (function () {
  let term;
  let fitAddon;
  let dotnetRef;
  let opened = false;
  let pendingWrites = [];
  let dataHandlerDispose;
  const RETRY_LIMIT = 30; // 3 seconds @ 100ms

  function ensureXtermLoaded(attempt, callback) {
    if (window.Terminal) {
      if (!term) {
        term = new Terminal({
          convertEol: true,
          cursorBlink: true,
          scrollback: 5000,
          fontFamily: 'monospace',
          fontSize: 14
        });
        if (window.FitAddon?.FitAddon) {
          try { fitAddon = new window.FitAddon.FitAddon(); term.loadAddon(fitAddon); } catch {}
        }
      }
      callback();
      return;
    }

    // Inject local fallback script once if CDN failed
    if (attempt === 1 && !document.getElementById('xterm-fallback')) {
      const s = document.createElement('script');
      s.id = 'xterm-fallback';
      s.src = 'js/xterm.min.js';
      document.head.appendChild(s);
    }

    if (attempt < RETRY_LIMIT) {
      setTimeout(() => ensureXtermLoaded(attempt + 1, callback), 100);
    } else {
      console.error('xterm.js failed to load; giving up.');
    }
  }

  function bindData() {
    if (!term) return;
    // Prevent stacking duplicate handlers
    if (dataHandlerDispose) {
      try { dataHandlerDispose.dispose && dataHandlerDispose.dispose(); } catch {}
      dataHandlerDispose = null;
    }
    if (typeof term.onData === 'function') {
      dataHandlerDispose = term.onData(data => {
        if (dotnetRef) {
          dotnetRef.invokeMethodAsync('SendInput', data);
        }
      });
    } else if (typeof term.on === 'function') {
      dataHandlerDispose = term.on('data', data => {
        if (dotnetRef) {
          dotnetRef.invokeMethodAsync('SendInput', data);
        }
      });
    } else {
      console.warn('xterm API: neither onData nor on available for data events');
    }
  }

  function init(elementId, dotnet) {
    dotnetRef = dotnet;
    opened = false;
    ensureXtermLoaded(0, () => {
      const el = document.getElementById(elementId);
      if (!el) {
        console.error('Element for terminal not found:', elementId);
        return;
      }
      if (!opened) {
        term.open(el);
        opened = true;
        bindData();
        term.focus();
        setupAutoResize(el);
        if (fitAddon) { try { fitAddon.fit(); } catch {} }
        if (pendingWrites.length) {
          pendingWrites.forEach(w => term.write(w));
          pendingWrites = [];
        }
      }
    });
  }

  function write(data) {
    if (!data) return;
    if (term && opened) {
      term.write(data);
    } else {
      pendingWrites.push(data);
    }
  }

  function clear() { if (term && opened) term.clear(); }
  function refit() { if (fitAddon && opened) { try { fitAddon.fit(); } catch {} } }
  function setupAutoResize(container) {
    function adjust() {
      const top = container.getBoundingClientRect().top;
      const h = window.innerHeight - top - 20; // 20px bottom padding
      if (h > 100) container.style.height = h + 'px';
      refit();
    }

    window.removeEventListener('resize', adjust);
    window.addEventListener('resize', adjust);
    // MutationObserver optional if layout shifts
    adjust();
  }

  return { init, write, clear, refit };
})();