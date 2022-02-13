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
        "JobTypes": {
            "MyType0": {
                "Description": "This is the initial job type for a 2 stage processing",
                "ResultBecomes": "MyType1"
            },
            "MyType1": {
                "Description": null,
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
        },
        "WorkerTracking": false,
        "BatchSize": 100,
        "PathBase": "/prefix"
        "DisableHttpsRedirect": false,
        "SSLOffload": false
    }

Job Types
=========

Job types are an optional feature specified with the :code:`JobTypes` field in the configuration.  The entire section can be omitted or set to :code:`null` if the feature isn't being used.

The primary reason to use this feature is to allow the results of one type of job to become new jobs of a different type without requiring a round trip out and back from the data store.  This is only useful if you have heterogenous workers which cannot process a single job through to completion.

Job types must have a unique string name which will be used to identify them to both clients and workers through the interface and endpoint URLs.  Optionally, a job type can have a :code:`"ResultBecomes"` field which must be a valid name of another job type. Do not create circular references.

.. code-block:: json

    "JobTypes": {
        "MyType0": {
            "Description": "This is the initial job type for a 2 stage processing",
            "ResultBecomes": "MyType1"
        },
        "MyType1": {
            "Description": null,
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

The simplest (and typically fastest) storage to set up is the local filesystem, however depending on how **GridLike** is deployed this may not be scalable or even feasible.  This can be an easy way to use network storage (such as with a mounted PVC in a K8 cluster), though it shifts the burden of configuration to the deployment environment.

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