import { renderToString } from "react-dom/server";
import { expect, test } from "vitest";
import { App } from "./App";

test("App names the product", () => {
  expect(renderToString(<App />)).toContain("Flownodes");
});
