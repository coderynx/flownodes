import { expect, test } from "@playwright/test";

const username = "root";
const password = "correct horse battery staple";
const sessionCookie = "__Host-flownodes";

test("a User signs in through Identity and signs out again", async ({ page, context }) => {
  await test.step("the first visit goes through Identity's page and returns to the same URL", async () => {
    await page.goto("/alarms/7");
    await page.getByLabel("Setup token").fill(process.env.FLOWNODES_SETUP_TOKEN!);
    await page.getByLabel("Username").fill(username);
    await page.getByLabel("Password").fill(password);
    await page.getByRole("button", { name: "Create the first User" }).click();

    await expect(page.getByText(`Signed in as ${username}`)).toBeVisible();
    await expect(page).toHaveURL("/alarms/7");
  });

  await test.step("the browser holds only an HttpOnly session cookie", async () => {
    const cookie = (await context.cookies()).find(({ name }) => name === sessionCookie);

    expect(cookie).toMatchObject({ httpOnly: true, secure: true, sameSite: "Lax", path: "/" });
  });

  await test.step("signing out ends the session and asks for the password again", async () => {
    await page.getByRole("button", { name: "Sign out" }).click();

    await expect(page.getByRole("heading", { name: "Sign in to Flownodes" })).toBeVisible();
    const cookies = await context.cookies();
    expect(cookies.map(({ name }) => name)).not.toContain(sessionCookie);
  });

  await test.step("signing in again with the password works", async () => {
    await page.getByLabel("Username").fill(username);
    await page.getByLabel("Password").fill(password);
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page.getByText(`Signed in as ${username}`)).toBeVisible();
  });
});
