=============
Configuration
=============

**GridLike** is configured through the :code:`appsettings.json` file, like any ASP.NET 6 application. The following is an example of the general structure, see the sections below for further configuration details and options.

.. code-block:: json

    {
        "Logging": {
            "LogLevel": {
                "Default": "Debug",
                "Microsoft.AspNetCore": "Warning"
            }
        },
        "Database": {
            "Type": "sqlite",
            "ConnectionString": "Data Source=/tmp/database.db"
        },
        "Storage": {
            "Type": "filesystem",
            "Path": "/tmp/storage"
        },
        "Authentication": {
            "Worker": {
                "Type": "simple",
                "Token": "thisisatoken"
            },
            "Api": {
                "Type": "simple",
                "Key": "thisisakey"
            },
            "Dashboard": {
                "Type": "simple",
                "User": "user",
                "Password": "guest"
            }
        }
    }

Database provider
=================

The database stores metadata about jobs and workers. **GridLike** currently supports Sqlite, MySQL/MariaDB, and Postgres.

Storage provider
================

The storage provider is where the binary blobs for the job and result payloads are stored.  **GridLike** currently supports the local filesystem and S3 compatible object storage.

Filesystem
----------

The simplest (and typically fastest) storage to set up is the local filesystem, however depending on how **GridLike** is deployed this may not be scalable or even feasible.  However, this is an easy way to use network storage, though it shifts the burden of configuration to the deployment environment.

.. code-block:: json

    "Storage": {
        "Type": "filesystem",
        "Path": "/path/to/storage"
    }

S3 Compatible
-------------

Amazon S3 and other S3 compatible HTTP accessible object stores are very easy to use and are typically scalable, although they create an additional two-way network trip to access data. In a containerized environment they're a good solution as long as file sizes aren't enormous.


.. code-block:: json

    "Storage": {
        "Type": "S3",
        "Endpoint": "s3.us-east-2.amazonaws.com",
        "AccessKey": "myaccesskey",
        "SecretKey": "mysecretkey",
        "Bucket": "mybucketname",
        "Ssl": true
    }

.. note::

    Minio is a good self-hosted option for S3 compatible storage if you need to set up a system entirely on-prem.


Authentication
==============

Generally speaking, you should not be deploying **GridLike** (or *any* network-accessible service, for that matter) without any authentication unless you're doing it entirely within a protected network environment. However, since authentication can be one of the more painful components of a web service to configure, **GridLike** has several simple built in components to get you up and running.

**GridLike** controls authentication separately for the following three components:

* Workers
* The job API
* The web dashboard

Each component has a separate entry in the :code:`"Authentication"` section of the configuration.  For any component, authentication can be turned off by specifying :code:`"Type": null`, though that is *not recommended*.



.. warning::

    Authentication is essentially useless if you're not using HTTPS. **GridLike** should be running with HTTPS enabled or be behind an SSL terminating proxy.  