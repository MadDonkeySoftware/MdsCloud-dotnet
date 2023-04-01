# MDS Cloud

Welcome to the MadDonkeySoftware Cloud repository. The main goal of MDS Cloud is to provide
developers and hobbyist a cloud-like environment that can be run on modest hardware. MDS Cloud
also aims to be transparent with what is happening across the various services. Lastly, the
MDS Cloud stack is meant to be hardware and OS agnostic.

## History

MadDonkeySoftware Cloud spawned from a learning experiment to create a state machine atop of
another open source functions-as-a-service (FaaS) project. After the initial prototype was
completed a desire to use the state machine / FaaS pairing to coordinate bulk processing
subtitle and video data on a home network. The original author opted to keep these workloads
inside of their home network rather than pay for utilizing a public cloud. As a few more
service were built the MadDonkeySoftware Cloud began to form.

## Quick Start

**NOTE:** Currently the quick start assumes you will be running MDS Cloud on your local system
in an effort to experiment or explore the environment. Once the system is in a state where the
MDS Cloud services may be deployed already this section will be re-written to reflect these
conditions accordingly.

This section also currently assumes that you have the source checked out locally, the solution
has been compiled and that the mds CLI has been symlinked into a area that is executable from
your terminal. As development matures and packages / binaries are made available this process
will be refined.

### First Time

- `mds stack init`
  - This will walk you through some required items so that further stack commands will operate properly
- `mds stack config`
  - Note: It is suggested to run this at least once but is not required for every stack up/down operation.
  - Allows a user/developer to select which services they would like to run in what modes.
    - Currently `local` and `localDev` are the only options that are functional
    - Currently `Identity` is the only service that is currently functional
- `mds stack build`
  - Generates all needed configs, keys, etc. to run MDS Cloud in a isolated environment on the system
- `mds stack up`
  - Starts the MDS Cloud stack and all supporting services in docker.
  - Now you should be able to run things like `mds id register` and `mds id token`!
- `mds config wizard`
  - (Optional) include a `--env [name]` where "[name]" is something you wish. This will default to `default` if not provided
  - This walks you through a series of prompts to create your first configuration for the CLI
- `mds stack down`
  - Halts the MDS Cloud stack and all supporting services in docker.

### Subsequent Times

While the above "First Time" section assumes you have never run MDS Cloud locally many of the
operations above can be omitted.

- `mds stack config`, `mds stack init`, and `mds stack build` can be run if one wants to change a services run mode.
- `mds stack up` and `mds stack down` can be run if one wants to re-use previously configured options.

## Development Prerequisites

The following components must be installed and available on the system to contribute with MDS
Cloud development. Feel free to install these items via their direct links or your package
manager of choice. If you can run `docker compose`, `dotnet` and `make` from your command line
you should be ready.

- [Docker Compose](https://docs.docker.com/compose/) installed.
- [.Net 6.0 +](https://dotnet.microsoft.com/en-ui/download)
  - NOTE: This project is actively developed from a non-windows system!

## Allowing mdsCloud to use insecure docker registry

MDS Cloud in a box uses an insecure docker registry to quickly get users up and
running. The IP addresses used by docker are in a "non-routable" IP range for
added safety. Since MDS uses your local systems docker instance by passing the
docker socket into the container that need it you will need to configure your
systems docker instance to allow insecure registries.

### Required changes

* edit the `/etc/docker/daemon.json` on your host system.
    * If this file does not exist, create it.
    * If you are on MacOS use the GUI to update your config.
* add/edit the below code block
    * The below networks are CIDR notation of the IPv4 non-routable address spaces
* restart docker

```
{
  "insecure-registries": [
    "10.0.0.0/8",
    "172.16.0.0/12",
    "192.168.0.0/16"
  ]
}
```

## Configuration of MDS CLI environments

The MDS Cloud CLI can be configured to handle multiple environments with most
commands via the `--env` argument. A new environment can be easily configured
by running the wizard: `mds config wizard --env local`. 

Below is a reference to wizard prompts if one were configuring a local environment.

| Prompt                          | Description                                           |
|---------------------------------|:------------------------------------------------------|
| Account                         | The account number received after registration        |
| User ID                         | The user id to be used with the system                |
| Password                        | the Password associated with the above user.          |
| Identity Service URL            | `https://127.0.0.1:8081`                              |
| Allow self signed certs.        | `Yes` since local uses an un-trusted self-signed cert |
| Notification Service URL        | `https://127.0.0.1:8082`                              |
| Queue Service URL               | `https://127.0.0.1:8083`                              |
| File Service URL                | `https://127.0.0.1:8084`                              |
| Serverless Function Service URL | `https://127.0.0.1:8085`                              |
| State Machine Service URL       | `https://127.0.0.1:8086`                              |
