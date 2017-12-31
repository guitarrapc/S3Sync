#!/bin/sh
cd `dirname $0`
cd source/S3Sync/obj/Docker/publish/
tar zcvf ../s3sync_netcore.tar.gz *