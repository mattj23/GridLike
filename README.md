# GridLike - Improvised Grid-like Compute

*Maybe you don't have or need a full computing grid.  Maybe you just need something with grid-like properties.  In that case, GridLike may be right for you.*


**GridLike** is a simple HTTP/Websocket based central work queue server to serve as the middleware for an improvised grid-like system for distributed computational workloads.  It acts as both a job registry and a work orchestrator, distributing binary payloads directly to *workers* who have registered to it, and receiving binary payloads back from them.  It retains these using a storage backend so they can be retrieved or sent back to the clients who submitted them.

## Overview

### Why GridLike?

**GridLike** exits because not all of us have access to a HPC cluster.

* It is HTTP based for easy routing in heterogenous environments (see [Why HTTP?](#why-http))
* It is easy to stand up and tear down, especially if you use containers
* It supports TLS and standard authentication practices so you're not leaking data everywhere

Compared to systems like [Work Queue](https://github.com/cooperative-computing-lab/cctools), **GridLike** is easier to deploy in mixed environments, can take advantage of HTTP routing, and can be integrated directly with clients written in any language that has a Websockets library.

Compared to systems like [Fireworq](https://github.com/fireworq/fireworq), **GridLike** doesn't require workers to have accessible HTTP endpoints, rather **GridLike** workers act like browsers and can thus transparently establish connections through NAT and firewalls.

**GridLike** is meant for embarrassingly parallel computational problems that would be feasible if you just had more compute resources, but aren't necessarily worth the trouble of getting access to or adapting them to run on a HPC cluster.  Instead you just might want to launch a bunch of containers on AWS Fargate or Google Cloud Run, or spin up some EC2 or GCP virtual machines, or connect a bunch of old laptops and the forgotten server in the closet...or maybe some combination of the above...and have a way to orchestrate all of that without a lot of effort.

***GridLike** was built because I needed something to run very large sets of robot kinematic and collision analyses. It has successfully been used for this purpose in hetrogenous environments.*

### Why not GridLike?

**GridLike** is not the same as having a cluster.  *Workers* don't have a built-in mechanism for transferring data between aside from through the central server.  

If you need fast interconnects or a have a lot of data being transferred compared to the corresponding amount of process runtime, **GridLike** is constrained by the network it's on and the HTTP/Websockets protocol.

If you need to run a problem on a massive scale, with tens of thousands of nodes or more and all of the associated costs, it's probably worth first trying established tools like [Work Queue](https://github.com/cooperative-computing-lab/cctools).

### Why HTTP?

The short answer is: because it's everywhere and all of our modern network infrastructure was built around it.

HTTP might seem like a bizarre choice for connecting worker nodes in a scientific computing application.  However, the incredible scale of HTTP adoption as the native protocol of the public internet means that all of our network infrastructure...from cloud load balancers to consumer routers...were built with it in mind.  

This means that **GridLike** can be deployed anywhere that someone imagined standing up a web server, with standard web server tools, and accessed by workers and clients from anywhere that a network engineer imagined someone might have a browser.  There are no custom ports, protocols, firewall or NAT rules to deal with, and the server can take advantage of all of tools built for HTTP like reverse and authenticating proxies.

Practically every common programming language has either built in or easily available open-source third party libraries for HTTP/HTTPS and Websockets, making it easy to write or adapt workers that can directly interact with the central server, rather than being invoked by a third party agent (though that is still an option).

The price of all of this is the overhead of the HTTP protocol.  Whether or not that makes sense for your particular compute problem is something that you need to consider.

#### Ok, but why Websockets?

Why go through the trouble of using Websockets?

Initially, the problem I was faced with that lead to the creation of **GridLike** pushed me towards a solution where I could have jobs start instantly when they were availible, rather than waiting for a worker to connect with a polling HTTP request.  Because Websockets wasn't difficult to get working in my environment I stuck with it so that the server could push jobs directly to the workers.

It would be possible to implement a traditional HTTP endpoint which non-websocket based workers could access through polling (or long-polling, perhaps).  This hasn't been done but if it's useful I would be willing to look at adding it.

## Getting Started



