# Environment variable to set

* APPINSIGHTS_INSTRUMENTATIONKEY: AppInsight 
* STORAGEACCOUNT_CONTAINER_NAME: name of the container that contains the processed tweets
* STORAGEACCOUNT_NAME: name of the storage account
* STORAGEACCOUNT_KEY: access key of the storage account


# How to build locally

docker build -t fzthapp .

# How to run locally
docker run --rm -e APPINSIGHTS_INSTRUMENTATIONKEY=e72cb901-6250-4068-8992-0e62c3bbddbd -e STORAGEACCOUNT_CONTAINER_NAME=output -e STORAGEACCOUNT_NAME=fzthlabsto -e STORAGEACCOUNT_KEY=nxUqI94qhRvU9lq3JkOTu8u4UHO1LMQHd+7hpPngI00fU29jVTyIVK5A+Zga0JgNupC0lo84ipS3FMyGDfa9kw== -it -p 8000:8000/tcp fzthapp:latest