(() => {
  const key = "krt-appearance";
  const system = window.matchMedia("(prefers-color-scheme: dark)");
  const normalize = value => ["light", "dark", "system"].includes(value) ? value : "system";
  const read = () => {
    try { return normalize(localStorage.getItem(key)); }
    catch { return "system"; }
  };
  let preference = read();

  function apply() {
    const theme = preference === "system" ? (system.matches ? "dark" : "light") : preference;
    document.documentElement.dataset.theme = theme;
    document.querySelectorAll("select[data-theme-choice]").forEach(select => { select.value = preference; });
    window.dispatchEvent(new Event("krt-theme-change"));
  }

  window.krtTheme = {
    getPreference: () => preference,
    setPreference(value) {
      preference = normalize(value);
      try {
        if (preference === "system") localStorage.removeItem(key);
        else localStorage.setItem(key, preference);
      } catch { /* The current page remains usable when storage is unavailable. */ }
      apply();
    },
  };

  apply();
  system.addEventListener("change", apply);
  window.addEventListener("storage", event => {
    if (event.key === key || event.key === null) { preference = read(); apply(); }
  });
  document.addEventListener("DOMContentLoaded", apply, { once: true });
  document.addEventListener("change", event => {
    if (event.target instanceof HTMLSelectElement && event.target.matches("[data-theme-choice]"))
      window.krtTheme.setPreference(event.target.value);
  });
})();
