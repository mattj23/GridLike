=================
GridLike Concepts
=================

Unlike a real compute grid, which has a complex set of interconnected compute resources managed by equally complex scheduling and job dispatch middleware designed to execute grid-unaware workloads on unsuspecting machines, **GridLike** uses a very simple operational model.

With **GridLike**, "jobs" are effectively a payload about which **GridLike** has no knowledge and simply handles as a binary blob. Jobs are submitted to the system through a HTTP endpoint and stored together with only a simple priority model.  "Workers" are **GridLike**-aware client programs which are given job payloads when they're available. "Results" payloads returned from the worker, which can be retrieved by interested clients through the HTTP API.

The main design tradeoff is requiring workers to understand the **GridLike** API.  By doing so workers effectively become homogenous and the need for complex orchestration (like specialized job description languages) goes away.  If you are building the binary which performs the computation, this is not much of a burden, and may in fact make your life easier as **GridLike** effectively hands input to your running process and carries away your output, saving you from having to manage that yourself. However, if you're trying to run someone else's binary, you need to write a "worker" agent which invokes or wraps that binary as a subprocess and handles communication with **GridLike**.

.. note::
    If you're writing a binary in C++, a GridLike client framework using IXWebSockets is available.
   
Jobs
====

A job is a binary payload and its associated metadata.  Jobs are submitted to **GridLike** by posting multipart form data to an HTTP endpoint.  The client can supply a unique string which will be used to identify the job, or by excluding a key one will be generated on the server.

Note that the job payload does not strictly *need* to be binary, **GridLike** will simply treat it as a stream of bytes regardless of what format it is. It is up to you to make sure that whatever is being submitted to **GridLike** is in the format that the worker expects to be given directly.

The job payload will be sent to whatever storage backend has been configured. Job metadata, which is stored in **GridLike**'s data provider, consists of the following:

* Unique key: a string uniquely identifying the job
* Job type: a string identifying the "type" of job
* Status: a job can be "Pending", "Running", "Done", or "Failed"
* Priority: a job can be submitted in one of two priority modes, "Immediate" or "Batch".  All "Immediate" jobs take precedence over "Batch" jobs.
* Submission time
* Start time
* Completion time
* Failure Count 
* Worker Id

Job Types
---------

Jobs may have different "types". Types allow segregation between different types of jobs and workers, but are primarily meant as a mechanism for simple multi-stage processing. Types must be configured in **GridLike**'s setup, and job submitters and workers must use the appropriate endpoint for the job type they are working with. Types can be configured such that the "result" of one type of job automatically becomes a job of a different type, saving the effort and bandwidth of having a third party client retrieve the result of one job and re-submit it as a different type of job.

This is only useful if you have multi-stage processing which cannot be done by the same worker, otherwise it is better to have one worker perform the entirety of the processing and save an entire round-trip of result transmission.

.. warning::
    **GridLike**'s philosophy of use is aligned with modern devops principles: everything is disposable and so should be simple to set up and tear down. While the "types" mechanism does mean that a single server can segregate different workloads and potentially handle multiple clients and users, it's best only to use it for multi-stage processing. The better segregation strategy is to simply deploy multiple **GridLike** instances as you need them and delete them when you're finished. Because all configuration is done through static files, **GridLike** instances should be easy to copy, modify, deploy, and delete.

Workers
=======

Workers are programs which connect to **GridLike** by establishing a Websocket connection through an HTTP endpoint.