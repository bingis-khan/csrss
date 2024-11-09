# CSRSS - my cute uwu C# RSS reader

I realized I want to kind of remember some pretty good sites with okay articles.
I habitually ignore my note taking app, so maybe making a publicly accessible RSS reader will help.


## Plan

- [X] Basic RSS display and periodic reading.
- [ ] Read servers from a file, more details, better design.
- [ ] Store RSS data and diff changes. (eg. `<item>`s that disappeared or name changes?).


## Deploying (on my Alpine Linux)

> Check deploy.sh

Example OpenRC service (as `/etc/init.d/csrss`):

```sh
#!/sbin/openrc-run

# openrc service for my RSS reader

name="csrss"
command="dotnet"
command_args="/usr/local/bin/csrss/csrss.dll --urls=http://localhost:6969"
pidfile="/run/${RC_SVCNAME}.pid"
command_background=true
```

To enable it I run:

```sh
rc-update add csrss default
```

To start it, I run:

```sh
rc-service csrss start
```
