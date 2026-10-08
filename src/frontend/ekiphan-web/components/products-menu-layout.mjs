export function menuBounds(viewportWidth, viewportHeight, triggerLeft, triggerBottom) {
  const width = Math.min(880, Math.max(0, viewportWidth - 32));
  const left = Math.max(16, Math.min(triggerLeft, viewportWidth - width - 16));
  const top = Math.max(0, Math.min(triggerBottom, viewportHeight - 80));
  return { width, left, top, maxHeight: Math.max(0, viewportHeight - top - 16) };
}

export function categoryPath(categories, id) {
  const path = [];
  const seen = new Set();
  let current = categories.find(item => item.id === id);
  while (current && !seen.has(current.id)) {
    seen.add(current.id);
    path.unshift(current);
    current = categories.find(item => item.id === current.parentId);
  }
  return path;
}

export function createHoverIntent(select, delay = 125) {
  let timer;
  let candidate;
  const cancel = () => { clearTimeout(timer); timer = undefined; candidate = undefined; };
  return {
    cancel,
    schedule(id) {
      if (candidate === id) return;
      cancel();
      candidate = id;
      timer = setTimeout(() => { cancel(); select(id); }, delay);
    },
  };
}
