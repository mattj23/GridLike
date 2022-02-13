=================
Operational Model
=================

The following is a detailed technical description of the operational model of **GridLike**, and is intended primarily for developers and the curious.  It is not necessary to know the following information in order to use **GridLike**.

Jobs and the Job Registry
=================================

Jobs are simply binary payloads delivered to **GridLike** through one of the HTTP endpoints as a multipart form file upload.  When uploaded, the job payload is sent to the storage backend and metadata is generated that needs to be put into a centralized job registry.

Job Types
---------

Job Types are an optional configuration element that can be specified in the **GridLike** server setup. If enabled, the server will recognize a list of unique strings which identify different "types" of jobs, and both job submission and worker connection will be segregated by job type.  Jobs types can be configured such that completed results for one type automatically become new jobs of another type.

Job Upload
----------

Jobs are uploaded to the :code:`api/jobs/submit` endpoint.  The multipart form data must contain the binary payload and a priority string (either "immediate" or "batch").  Optionally, a string description (up to 128 characters) and a string key can be supplied as well.

If job types are configured on the server, the endpoint becomes :code:`api/jobs/submit/{job_type}`

Internally, jobs are referenced only by a GUID which is generated on the server at the time the job is submitted.  However, to clients of the API and the web interface, the string key can be substituted to offer a more human readable identifier.

If the string key is supplied, it must be unique.  If it is not unique, a 400 bad request will be returned from the server.  If the key is not supplied, the job's GUID will serve in place of the key text in both the API and the web interfaces.

Jobs are assigned to a "batch", which is a group of configurable maximum size which share a common integer batch ID. Batches only contain jobs of a single type, and are created sequentially. When a job is submitted it is assigned to the current batch for that job type. When the batch has reached the maximum size, no new jobs will be included in that batch and queries by the batch ID can be considered stable.

If the job is successfully submitted, the binary payload is sent to the storage backend and stored by the GUID.  A metadata entry is generated with the GUID, the batch ID, the key, the priority, and the current UTC time as the submission time.  The metadata status is set to "pending", and sent to the job registry.

Job Registry
------------

The job registry is a persistent storage of job metadata.  

The registry has two conflicting objectives: it needs to maintain a reliable and consistent store of job information, and it needs to access this information very quickly.  Confounding this is the potential for there to be a very large number of jobs, and submission requests will be coming in asynchronously.

Currently the registry uses a very simple Entity Framework Core model which can be persisted to Sqlite, MySQL/MariaDB, and PostgreSQL.  Theoretically any provider supported by EFCore could be added, but something like Redis might be a better solution overall.  

The registry must be able to perform the following operations with thread-safety and data consistency:

* Check uniqueness of a job key
* Add (submit) a job. This assumes that the binary data has already been put in the storage backend
* Remove a job
* Update the status of a job (pending, running, complete, failed) with associated properties
* Determine the next *n* jobs in order of priority

The registry must be able to retrieve data for all jobs, however this data only needs to be eventually consistent, as it is not used for scheduling.

The registry must also persist any critical data such that if the **GridLike** server goes down:

* No job metadata/payload data is lost
* Job status updates are allowed to be lost as long as it does not cause a job to be stuck in an incomplete state
* Job results and status should not be lost if they've been complete and stable for more than a few seconds

Workers and the Job Dispatcher
==============================

Workers and Worker Types
------------------------

Workers are programs running remotely which connect to the :code:`api/worker` endpoint and request a Websocket connection. Websockets allow for two different types of messages: binary and text.  The server and the worker communicate with each other via json-serialized simple messages over the text channel, while the binary channel is reserved for job and result payloads.  Any binary message sent from the server to the worker is a job payload, and any binary message sent from the worker to the server is a result payload for the last job the worker was sent.

If job types are configured on the server, the endpoint becomes :code:`api/worker/{job_type}`

Notes on Websockets
^^^^^^^^^^^^^^^^^^^

Websockets allow multidirectional communication between client and server, though in heavily NAT'ed and/or firewalled environments they do require keepalive messages for the network hardware between them to keep the bidirectional connection alive.

With a Websocket connection, if the server goes down the client will notice the disconnection almost immediately, and the connection will be terminated. Even if the server comes back up before the worker has finished its computation, the server only knew the recipient of the particular job payload by the association between the open Websocket connection and the worker. Thus the result of the computation will be lost regardless.

Worker/Server Communication
---------------------------

Non-binary messages passed between the worker and the server are JSON formatted objects with a message code that determines the message type. The other fields in the object will depend on the message code.

.. list-table:: Message Codes
    :header-rows: 1

    * - Code
      - Message Type
    * - 0
      - Registration message
    * - 1
      - Status response
    * - 2
      - Status request
    * - 3
      - Progress update
    * - 4
      - Job failed


Workers must immediately send an authentication/registration message to the server after connecting, or they will be kicked by the server after a configurable amount of time (default 500ms). The registration message must contain three things:

1. A string containing a display name for the worker
2. A string containing a unique identifier for the worker, that will ideally remain the same between process restarts and/or machine reboots. This is primarily for tracking unique worker statistics.  On a modern linux machine, using the contents of :code:`/etc/machine-id` is sufficient.
3. A string containing a registration token.  If authentication is disabled on the server the contents of this field are irrelevant, but otherwise it is a pre-shared secret which authenticates the worker to the server. 

Registration message format: 

.. code-block:: json

    {
        "code": 0,
        "name": "machine name",
        "id": "unique-machine-id",
        "token": "super-secret-token-string"
    }

If the authentication is successful the server will query the worker for its status and worker management will begin.  If the authentication is not successful the Websocket connection will be closed after the grace period.

The format of the status request will be simply:

.. code-block:: json

    {
        "code": 2
    }

Whenever it receives a status request from the server, the worker should respond with a status response message, which contains a status code.

.. list-table:: Worker Status Codes
    :header-rows: 1

    * - Code
      - Message Type
    * - 0
      - Busy
    * - 1
      - Ready

Status response message:

.. code-block:: json

    {
        "code": 1,
        "status": 0     
    }

At any point during the processing of a job, the worker can (but is not obligated to) send progress updates to the server. This is done using the progress message, which can be used to send a percent complete, an informational text message, or both.

Progress message (sent from worker to server):

.. code-block:: json

    {
        "code": 3,
        "percent": null,    
        "info": "reticulating splines"
    }

The :code:`"percent"` field should either be :code:`null` or a floating point value between :code:`0.0` and :code:`100.0`. The :code:`"info"` field can be either :code:`null` or a string containing a status message that will be displayed to any observing clients.  Both fields cannot be simultaneously :code:`null`.

Lastly, if an error occurs during the processing of a job payload, the worker should send a message to the server indicating that the job failed.  This message can optionally include an information message and/or a text field containing more detailed log information.

.. code-block:: json

    {
        "code": 4,
        "logs": null,    
        "info": null
    }

Worker Manager
--------------

The worker manager is the component which handles all interactions with workers after they have established a connection to the server. It's the entity on the other side of the worker/server communication discussed in the section above.

The worker manager must maintain an up-to-date list of the currently connected (and recently disconnected) workers, as well as track their current state.  The manager must be able to, in a thread-safe way: 

* Enroll a worker upon connection, retrieving its identity if it has one
* Retrieve the current list of connected workers 
* Retrieve the last known state of any worker
* Kick workers which are unregistered after a configurable amount of time
* Receive status, progress, and failure messages from workers and update their status accordingly
* Query workers for their status when it is unsure or has outdated information
* Keep track of the number of failed and successful jobs on a worker


Job Dispatcher
--------------

As its name implies, the job dispatcher is responsible for all dispatching of jobs to available workers.  It is the conceptual bridge between the jobs and the pool of workers. It cannot interact with either directly, and must do so through the job registry and worker manager, respectively.

The job dispatcher must perform the following in a thread-safe way:

* Retrieve the highest priority jobs from the registry
* Retrieve available workers from the the worker manager
* Individually assign jobs to workers (avoiding any race conditions)
* Determine when a job has finished and invoke a state change in the registry
* Determine when a job has failed and invoke a state change in the registry
* Dispatch failed jobs to a different worker than the one they last failed on
* Lower the priority of failed jobs so that they don't clog up the work queue
* Determine when a worker working on a job disconnects and reset the job
* Determine when jobs appear to be stuck
