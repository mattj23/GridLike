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
        },
        "ServerOptions": {
            "WorkerTracking": false,
            "BatchSize": 100,
            "JobTypes": {
                "Type0": {
                    "Description": "Initial job type",
                    "ResultBecomes": "Type1"
                },
                "Type1": { }
            }
        },

        "PathBase": "/prefix"
        "DisableHttpsRedirect": false,
        "SSLOffload": false
    }

Server Configuration Options
============================

The server configuration options are a server-wide set of static properties which determine miscellaneous aspects of how **GridLike** behaves.

* :code:`"WorkerTracking"` is an optional parameter which if left :code:`null` or omitted will default to :code:`true`. If worker tracking is on, workers will be tracked by their unique IDs allowing job statistics to be queried.  Disable this feature if your workers will not be connecting to the server with stable unique IDs.

* :code:`"BatchSize"` is an optional parameter which if left :code:`null` or omitted will default to 100. It specifies the maximum size of a batch before no new jobs will be added to it.


Job Types
---------

Job types are an optional feature specified with the :code:`JobTypes` field in the server configuration options section.  The entire sub-section can be omitted or set to :code:`null` if the feature isn't being used.

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
        },
        "MyType2": { }
    }

.. warning::

    Job Types are specified in the configuration file but stored in the database. If the database is empty, any relevant job types will be created, however if the database is not empty the server will validate that the database contents matches the server configuration on startup and throw an error if they do not match.



Database provider
=================

The database stores metadata about jobs and workers. **GridLike** currently supports Sqlite, MySQL/MariaDB, and PostgreSQL.  The database schema will be created on startup if it does not exist.

Sqlite configuration example:

.. code-block:: json

    "Database": {
        "Type": "sqlite",
        "ConnectionString": "Data Source=/path/to/database.db"
    },

MySQL/MariaDB configuration example:

.. code-block:: json

    "Database": {
        "Type": "mysql",
        "ConnectionString": "server=127.0.0.1;uid=root;pwd=12345;database=test"
    },

PostgreSQL configuration example:

.. code-block:: json

    "Database": {
        "Type": "postgresql",
        "ConnectionString": "Server=127.0.0.1;Port=5432;Database=myDataBase;User Id=myUsername;Password=myPassword;"
    },



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

Worker Authentication
---------------------

Worker authentication requires a connected worker to send a special Websocket message containing some information about itself and an authentication token.  Currently, a simple token provider is available and can be enabled with the following configuration example.  Notice that the :code:`"KickAfterMs"` field denotes the number of milliseconds after which an unauthenticated worker will have the connection closed by the server.
        
.. code-block:: json

    "Authentication": {
        "Worker": {
            "Type": "simple",
            "Token": "thisisatoken",
            "KickAfterMs": 500
        },
        
    },

API Authentication
------------------

The worker connection API allows anonymous access because the server requires authentication after a Websocket connection has been established, but the Job API is a traditional HTTP endpoint and should be protected.  Currently a simple authentication token provider is available and can be enabled with the following configuration example. This will require the clients to set a :code:`X-API-KEY` header in the HTTP request with the value set in the configuration.

.. code-block:: json

    "Authentication": {
        "Api": {
            "Type": "simple",
            "Key": "thisisakey"
        },

    },

Web Dashboard Authentication
----------------------------

The web dashboard is primarily a view-only interface, but it should still be protected if **GridLike** is exposed to a public network. Currently a simple username/password authentication mechanism is provided and can be enabled with the following configuration:

.. code-block:: json

    "Authentication": {
        "Dashboard": {
            "Type": "simple",
            "User": "user",
            "Password": "guest"
        }
    },