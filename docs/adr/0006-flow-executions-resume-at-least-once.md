# Flow Executions checkpoint per Node and resume at-least-once

A Flow Execution is checkpointed after every completed Node, so when its Silo fails it resumes at the last checkpoint on another Silo, and waits survive failover. We chose this over restarting from the trigger or dropping the Execution because HA (ADR-0001) is meaningless for automation that silently stops halfway through, for example after locking a train's doors but before telling the driver.

## Consequences

- An Action can run more than once when a Silo fails between invoking it and checkpointing. Integrations should make Actions safe to repeat where they can, and Flow authors must not assume exactly-once.
