#!/bin/bash

kafka-topics --create --if-not-exists --bootstrap-server $1 --replication-factor 1 --partitions 1 --topic message-received