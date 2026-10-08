import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { categoryPath, createHoverIntent, menuBounds } from "./products-menu-layout.mjs";

for (const width of [320, 375, 768, 1280, 1440]) {
  test(`menu stays within ${width}px viewport geometry`, () => {
    for (const height of [568, 720, 900]) {
      for (const left of [0, width / 2, width - 20]) {
        const bounds = menuBounds(width, height, left, 184);
        assert.ok(bounds.left >= 16);
        assert.ok(bounds.left + bounds.width <= width - 16);
        assert.ok(bounds.top + bounds.maxHeight <= height - 16);
      }
    }
  });
}

const categories = [
  { id: "root", parentId: null, slug: "root" },
  { id: "two", parentId: "root", slug: "two" },
  { id: "three", parentId: "two", slug: "three" },
  { id: "four", parentId: "three", slug: "four" },
  { id: "other", parentId: null, slug: "other" },
];
test("initial state has no selected category or automatic branch", () => {
  assert.deepEqual(categoryPath(categories, null), []);
});
test("four levels stay in the chosen root branch", () => {
  assert.deepEqual(categoryPath(categories, "four").map(item => item.id), ["root", "two", "three", "four"]);
  assert.deepEqual(categoryPath(categories, "other").map(item => item.id), ["other"]);
});
test("malformed cycle cannot loop forever", () => {
  assert.equal(categoryPath([{ id: "a", parentId: "b" }, { id: "b", parentId: "a" }], "a").length, 2);
});

test("desktop panel height and columns do not depend on category selection", () => {
  const css = readFileSync(new URL("./products-menu.module.css", import.meta.url), "utf8");
  const desktop = css.split("@media(min-width:901px) {")[1].split("@media(max-width:900px)")[0];
  assert.match(desktop, /height:min\(26rem,var\(--menu-height/);
  assert.match(desktop, /\.desktop,\.desktop\[data-selected=true\]/);
  assert.match(desktop, /grid-template-rows:minmax\(0,1fr\)/);
  assert.match(desktop, /\.childList \{ flex:1;/);
});

test("hover waits 125ms and repeated pointer moves do not restart it", context => {
  context.mock.timers.enable({ apis: ["setTimeout"] });
  const selections = [];
  const hover = createHoverIntent(id => selections.push(id));
  hover.schedule("root");
  context.mock.timers.tick(100);
  hover.schedule("root");
  context.mock.timers.tick(24);
  assert.deepEqual(selections, []);
  context.mock.timers.tick(1);
  assert.deepEqual(selections, ["root"]);
});

test("changing or leaving a category cancels stale hover selections", context => {
  context.mock.timers.enable({ apis: ["setTimeout"] });
  const selections = [];
  const hover = createHoverIntent(id => selections.push(id));
  hover.schedule("root");
  context.mock.timers.tick(100);
  hover.schedule("other");
  context.mock.timers.tick(125);
  assert.deepEqual(selections, ["other"]);
  hover.schedule("four");
  hover.cancel();
  context.mock.timers.tick(125);
  assert.deepEqual(selections, ["other"]);
});
