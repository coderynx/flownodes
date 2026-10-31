# Data Point History stays in memory; a Device's last known values are saved on a timer and restored as bad

Data Point History lives only in the memory of the Device and Asset grains and is lost when a Silo stops. A Device's last known values are saved on a timer and on graceful shutdown, and come back after a restart with Quality bad until the Device reports again. We rejected writing every reported value to Postgres, because one write per report is a disk load a Raspberry Pi on an SD card cannot reasonably host (ADR-0001). History can afford the loss because only people read it: Alarm Conditions and Flows act on live values, so an empty trend after a restart changes no automation. We rejected restoring values as uncertain or with their original Quality, because automation acts on those: an old pressure value could clear a live Alarm or raise a false one.

## Consequences

- A hard crash loses up to one save interval of last known values, and all Data Point History. An Integration to an external database is the way to keep values through a restart.
- Alarm Conditions and Flow `when` conditions ignore a restored value, so a Flow cannot act on a last known value until a new report arrives.
- A Flow trigger without `when` no longer fires on a Device's first report after a restart when the value has not changed.
- An Asset Data Point written by a Flow or a User stays good after a restart. Flownodes is itself the source of that value, so it is not an old reading from hardware. Mapped and computed Asset Data Points take their Quality from their inputs.
- Data Point History is bounded by a count of values per Data Point rather than by time, because a time bound cannot cap memory for a fast Data Point.
