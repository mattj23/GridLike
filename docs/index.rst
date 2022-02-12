.. GridLike documentation master file, created by
   sphinx-quickstart on Fri Feb 11 20:41:14 2022.
   You can adapt this file completely to your liking, but it should at least
   contain the root `toctree` directive.

========
GridLike
========

**GridLike** is a simple HTTP/Websocket based central work queue server to serve as the middleware for an improvised grid-like system for distributed computational workloads.  It acts as both a job registry and a work orchestrator, distributing binary payloads directly to *workers* who have registered to it, and receiving binary payloads back from them.  It retains these using a storage backend so they can be retrieved or sent back to the clients who submitted them.

**GridLike** is, at its core, an ASP.NET 6 web application which uses server-side Blazor to provide a minimal web interface that can be used to monitor job and worker status.  The majority of configuration is intended to be done through the application's static JSON configuration file.

**GridLike** has many modular components which can be selected in its static configuration, providing different options for file storage, database backend, and authentication.

.. toctree::
   :maxdepth: 2
   :caption: Contents:

   concepts
   configuration



Indices and tables
==================

* :ref:`genindex`
* :ref:`modindex`
* :ref:`search`
