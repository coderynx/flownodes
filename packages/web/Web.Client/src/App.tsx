import { useEffect, useState } from "react";

type Session = { id: string; name: string };

export function App() {
  const [session, setSession] = useState<Session>();

  useEffect(() => {
    void fetch("/bff/session").then(async (response) => {
      if (response.status === 401) {
        const returnUrl = window.location.pathname + window.location.search;
        window.location.assign(
          `/bff/signin?returnUrl=${encodeURIComponent(returnUrl)}`,
        );
      } else if (response.ok) {
        setSession((await response.json()) as Session);
      }
    });
  }, []);

  return (
    <>
      <h1>Flownodes</h1>
      {session && (
        <form method="post" action="/bff/signout">
          <p>Signed in as {session.name}</p>
          <button type="submit">Sign out</button>
        </form>
      )}
    </>
  );
}
