===============
Future Features
===============

The following are an unordered list of potential feature ideas to be implemented in the future.

Use S3 preauthorized links
--------------------------

Currently all binary data flows through **GridLike**. It would be possible, in large scale deployments which are using S3 for a storage backend with a provider that has extremely high bandwidth, to have **GridLike** serve only as a broker.  Clients submitting jobs would be given a preauthorized link to upload their data files to, and workers would receive a custom payload that contained a preauthorized download link to retrieve their input payload from, plus a preauthorized upload link to return their results to.

This would result in most data transfer happening against AWS or similar cloud servers, while **GridLike** exchanges only metadata with clients and workers.

Caching with Memcached or Redis
-------------------------------

When building **GridLike** initially I just needed to get something working so I took the easy way out and used EFCore to persist data. Though EFCore has gotten considerably faster since .NET 6 it's not necessarily the optimal solution for a fast job queue.  Something like Redis or memcached might be a better solution overall, either as an in-memory cache for jobs or as the primary store for data.  
