import json
import os
import time

import boto3
from botocore.config import Config
from botocore.exceptions import EndpointConnectionError, ReadTimeoutError

endpoint = os.environ["AWS_ENDPOINT_URL"]
config = Config(connect_timeout=2, read_timeout=3, retries={"total_max_attempts": 1})
sns = boto3.client("sns", endpoint_url=endpoint, config=config)
sqs = boto3.client("sqs", endpoint_url=endpoint, config=config)

for attempt in range(30):
    try:
        topic = sns.create_topic(Name="account-events")["TopicArn"]
        break
    except (EndpointConnectionError, ReadTimeoutError):
        if attempt == 29:
            raise
        time.sleep(1)

for consumer in ("fraud", "cards"):
    name = f"{consumer}-events"
    dead_letter = sqs.create_queue(QueueName=f"{name}-dlq")["QueueUrl"]
    queue = sqs.create_queue(QueueName=name)["QueueUrl"]
    queue_arn = sqs.get_queue_attributes(QueueUrl=queue, AttributeNames=["QueueArn"])["Attributes"]["QueueArn"]
    dead_letter_arn = sqs.get_queue_attributes(
        QueueUrl=dead_letter, AttributeNames=["QueueArn"]
    )["Attributes"]["QueueArn"]
    policy = {"Version": "2012-10-17", "Statement": [{
        "Effect": "Allow", "Principal": {"Service": "sns.amazonaws.com"}, "Action": "sqs:SendMessage",
        "Resource": queue_arn, "Condition": {"ArnEquals": {"aws:SourceArn": topic}}
    }]}
    sqs.set_queue_attributes(QueueUrl=queue, Attributes={
        "Policy": json.dumps(policy),
        "RedrivePolicy": json.dumps({"deadLetterTargetArn": dead_letter_arn, "maxReceiveCount": 5})
    })
    sns.subscribe(TopicArn=topic, Protocol="sqs", Endpoint=queue_arn,
                  Attributes={"RawMessageDelivery": "true"})
