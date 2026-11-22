/** URLs de despliegue (primaria con fallback). */
window.VENTORY_SHELL = {
  primaryUrl:
    (typeof VENTORY_PRIMARY_URL !== "undefined" && VENTORY_PRIMARY_URL) ||
    "https://ventory.bferal.cl",
  fallbackUrl:
    (typeof VENTORY_FALLBACK_URL !== "undefined" && VENTORY_FALLBACK_URL) ||
    "https://ventory.tasker.cl",
  postMessageChannel: "ventory-desktop",
};