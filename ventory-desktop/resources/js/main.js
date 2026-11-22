// Inyectado en ventory.*.cl solo cuando se usa Neutralino (neu run / .exe sin VentoryDesktop).
// La app Windows recomendada (VentoryDesktop.exe) no usa Neutralino ni este menú de bandeja.

const STORAGE_KEYS = {
  seenNotifyIds: "ventory.seenNotifyIds",
};

const TRAY_LAUNCH_FLAGS = new Set(["--tray-only", "--minimized", "--startup"]);

function argsIncludeTrayBoot() {
  return (NL_ARGS || []).some((arg) => TRAY_LAUNCH_FLAGS.has(String(arg).toLowerCase()));
}

function hookNotificationStream() {
  if (window.__ventoryDesktopHooked) return;
  window.__ventoryDesktopHooked = true;

  const NativeEventSource = window.EventSource;
  if (!NativeEventSource) return;

  window.EventSource = function VentoryEventSource(url, options) {
    const source = new NativeEventSource(url, options);
    source.addEventListener("message", (event) => {
      if (!event.data || event.data === ": heartbeat") return;
      let data;
      try {
        data = JSON.parse(event.data);
      } catch (_) {
        return;
      }
      if (data.type === "NOTIFICATION") {
        showDesktopNotification(data.payload || {});
      }
    });
    return source;
  };
  window.EventSource.prototype = NativeEventSource.prototype;
  window.EventSource.CONNECTING = NativeEventSource.CONNECTING;
  window.EventSource.OPEN = NativeEventSource.OPEN;
  window.EventSource.CLOSED = NativeEventSource.CLOSED;
}

async function showDesktopNotification(payload) {
  const id = String(payload.id || payload.message || "");
  const seenRaw =
    (await Neutralino.storage.getData(STORAGE_KEYS.seenNotifyIds).catch(() => "")) || "[]";
  let seen = [];
  try {
    seen = JSON.parse(seenRaw);
  } catch (_) {
    seen = [];
  }
  if (id && seen.includes(id)) return;
  if (id) {
    seen = [id, ...seen].slice(0, 200);
    await Neutralino.storage.setData(STORAGE_KEYS.seenNotifyIds, JSON.stringify(seen));
  }

  try {
    await Neutralino.os.showNotification(
      "VENTORY",
      payload.message || "Nueva notificación",
      { icon: "/resources/icons/trayIcon.png" }
    );
  } catch (err) {
    Neutralino.debug.log(`showNotification: ${err}`);
  }
}

function onWindowClose() {
  Neutralino.window.hide();
}

async function onReady() {
  hookNotificationStream();
  // Sin setTray en Windows: el menú al clic izquierdo es limitación de Neutralino.
  // Usar AppLocal/launch.ps1 → VentoryDesktop.exe para bandeja correcta.
  if (NL_OS !== "Windows" && NL_MODE === "window" && NL_OS !== "Darwin") {
    await Neutralino.os.setTray({
      icon: "/resources/icons/trayIcon.png",
      menuItems: [{ id: "SHOW", text: "Mostrar VENTORY" }, { id: "QUIT", text: "Salir" }],
    });
  }
  if (argsIncludeTrayBoot()) {
    await Neutralino.window.hide();
  }
}

Neutralino.init();
Neutralino.events.on("ready", onReady);
Neutralino.events.on("windowClose", onWindowClose);
Neutralino.events.on("trayMenuItemClicked", async (event) => {
  if (event.detail.id === "SHOW") {
    await Neutralino.window.show();
    await Neutralino.window.focus();
  } else if (event.detail.id === "QUIT") {
    await Neutralino.app.exit();
  }
});
hookNotificationStream();